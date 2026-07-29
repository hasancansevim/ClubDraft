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

    public byte[] RowVersion { get; private set; } // Optimistic Concurrency

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

        AddDomainEvent(new PlayerClaimedEvent(Id, clubId, playerId, pickNumber));

        CurrentPickIndex++;

        if (CurrentPickIndex >= _turnOrder.Count)
        {
            Status = DraftStatus.Completed;
            AddDomainEvent(new DraftCompletedEvent(Id));
        }
        else
        {
            AddDomainEvent(new DraftTurnAdvancedEvent(Id, _turnOrder[CurrentPickIndex], CurrentPickIndex));
        }
    }

    public void RevertClaim(Guid playerId)
    {
        // Compensating action for Saga
        var pick = _picks.LastOrDefault(p => p.PlayerId == playerId);
        if (pick == null)
            return; // Or throw

        var playerItem = _playerPool.FirstOrDefault(p => p.PlayerId == playerId);
        if (playerItem != null && playerItem.IsClaimed)
        {
            playerItem.RevertClaim();
        }

        var affectedClubId = pick.ClubId;
        _picks.Remove(pick);

        // ÖNEMLİ: CurrentPickIndex'i GERİ ALMIYORUZ.
        // Draft zaten ileri gitmiş olabilir; onun yerine etkilenen kulübe
        // sıradaki (CurrentPickIndex) pozisyona bir "makeup pick" hakkı ekliyoruz.
        _turnOrder.Insert(CurrentPickIndex, affectedClubId);
        
        // Eğer draft completed olmuşsa geri çekildiğinde durum InProgress'e dönmeli
        Status = DraftStatus.InProgress;

        AddDomainEvent(new PlayerClaimRevertedEvent(Id, playerId, affectedClubId));
    }
}
