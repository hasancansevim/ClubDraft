using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.Draft.Domain.Entities;
using ClubCraft.Draft.Domain.Enums;
using ClubCraft.Draft.Domain.Events;
using ClubCraft.Draft.Domain.ValueObjects;

namespace ClubCraft.Draft.Domain.Aggregates;

public class DraftSession : AggregateRoot<Guid>
{
    public Guid RoomId { get; private set; }
    public DraftStatus Status { get; private set; }
    
    private readonly List<Guid> _turnOrder = new();
    public IReadOnlyCollection<Guid> TurnOrder => _turnOrder.AsReadOnly();
    
    public int CurrentPickIndex { get; private set; }
    
    private readonly List<DraftPlayerPoolItem> _playerPool = new();
    public IReadOnlyCollection<DraftPlayerPoolItem> PlayerPool => _playerPool.AsReadOnly();
    
    private readonly List<DraftPick> _picks = new();
    public IReadOnlyCollection<DraftPick> Picks => _picks.AsReadOnly();



    private DraftSession() { } // EF Core

    public DraftSession(Guid roomId, IEnumerable<DraftPlayerPoolItem> players)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        Status = DraftStatus.Lobby;
        CurrentPickIndex = 0;
        _playerPool.AddRange(players);
    }

    public void StartDraft(List<Guid> turnOrder)
    {
        if (Status != DraftStatus.Lobby)
            throw new InvalidOperationException("Draft is already started or completed.");

        if (turnOrder == null || !turnOrder.Any())
            throw new ArgumentException("Turn order must be provided.", nameof(turnOrder));

        _turnOrder.AddRange(turnOrder);
        Status = DraftStatus.InProgress;

        AddDomainEvent(new DraftStartedEvent(Id));
        AddDomainEvent(new DraftTurnAdvancedEvent(Id, _turnOrder[0], 0));
    }

    public void ClaimPlayer(Guid clubId, Guid playerId)
    {
        if (Status != DraftStatus.InProgress)
        {
            AddDomainEvent(new PlayerClaimRejectedEvent(Id, clubId, playerId, "Draft is not in progress."));
            return;
        }

        var expectedClubId = _turnOrder[CurrentPickIndex];
        if (expectedClubId != clubId)
        {
            AddDomainEvent(new PlayerClaimRejectedEvent(Id, clubId, playerId, "It is not your turn."));
            return;
        }

        var playerItem = _playerPool.FirstOrDefault(p => p.PlayerId == playerId);
        if (playerItem == null)
        {
            AddDomainEvent(new PlayerClaimRejectedEvent(Id, clubId, playerId, "Player not found in pool."));
            return;
        }

        if (playerItem.IsClaimed)
        {
            AddDomainEvent(new PlayerClaimRejectedEvent(Id, clubId, playerId, "Player is already claimed."));
            return;
        }

        // Apply pick
        playerItem.MarkAsClaimed();
        var pickNumber = CurrentPickIndex + 1;
        var pick = new DraftPick(pickNumber, clubId, playerId, DateTime.UtcNow);
        _picks.Add(pick);

        var pickAttemptId = Guid.NewGuid();
        AddDomainEvent(new PlayerClaimedEvent(pickAttemptId, Id, clubId, playerId, pickNumber, playerItem.Snapshot.Name, playerItem.Snapshot.Position, playerItem.Snapshot.Overall, playerItem.Snapshot.Age, playerItem.Snapshot.MarketValue));

        CurrentPickIndex++;

        if (CurrentPickIndex >= _turnOrder.Count)
        {
            Status = DraftStatus.Completed;
            AddDomainEvent(new DraftCompletedEvent(Id, _turnOrder.Distinct()));
        }
        else
        {
            AddDomainEvent(new DraftTurnAdvancedEvent(Id, _turnOrder[CurrentPickIndex], CurrentPickIndex));
        }
    }

    public void RevertClaim(Guid pickAttemptId, Guid playerId)
    {
        // Saga compensating action — kadro limiti aşıldığında veya nadir bir hata durumunda tetiklenir.
        var pick = _picks.LastOrDefault(p => p.PlayerId == playerId);
        if (pick == null)
            return;

        var playerItem = _playerPool.FirstOrDefault(p => p.PlayerId == playerId);
        if (playerItem != null && playerItem.IsClaimed)
        {
            playerItem.RevertClaim();
        }

        var affectedClubId = pick.ClubId;
        _picks.Remove(pick);

        // ÖNEMLİ: CurrentPickIndex'i GERİ ALMIYORUZ — "makeup pick" tasarımı.
        // Revert anında draft başka pick'lerle ilerlemiş olabilir; index-- yapmak
        // o aralıkta yapılmış pick'lerin slot'larını bozar (race condition §4.7).
        // Bunun yerine etkilenen kulübe mevcut CurrentPickIndex pozisyonuna
        // bir "makeup" sırası ekliyoruz — hakkı kaybolmaz, sadece sonraya kayar.
        _turnOrder.Insert(CurrentPickIndex, affectedClubId);

        // Eğer draft completed olmuşsa InProgress'e geri dön
        Status = DraftStatus.InProgress;

        AddDomainEvent(new PlayerClaimRevertedEvent(pickAttemptId, Id, playerId, affectedClubId));

        // Yeni CurrentPickIndex'teki kulübe sıra bildirimi (Insert sonrası aynı index, aynı kulüp)
        if (CurrentPickIndex < _turnOrder.Count)
        {
            AddDomainEvent(new DraftTurnAdvancedEvent(Id, _turnOrder[CurrentPickIndex], CurrentPickIndex));
        }
    }

}
