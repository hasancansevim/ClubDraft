using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Common.Enums;
using ClubCraft.ClubManagement.Domain.Entities;
using ClubCraft.ClubManagement.Domain.Enums;
using ClubCraft.ClubManagement.Domain.Events;
using ClubCraft.ClubManagement.Domain.ValueObjects;

namespace ClubCraft.ClubManagement.Domain.Aggregates;

public class Club : AggregateRoot<Guid>
{
    public Guid RoomId { get; private set; }
    public Guid ParticipantId { get; private set; }
    public Guid PresidentUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money Budget { get; private set; } = Money.Zero;
    public string LineupJson { get; private set; } = "{}";
    public string Formation { get; private set; } = "4-4-2";
    
    private readonly List<Player> _roster = new();
    public IReadOnlyCollection<Player> Roster => _roster.AsReadOnly();
    
    private readonly List<WeeklyDecision> _weeklyDecisions = new();
    public IReadOnlyCollection<WeeklyDecision> WeeklyDecisions => _weeklyDecisions.AsReadOnly();

    public const int MaxRosterSize = 20;
    public const decimal DefaultInitialBudget = 5_000_000m;

    private Club() { } // EF Core

    public Club(Guid id, Guid roomId, Guid presidentUserId, string name, decimal initialBudget, Guid participantId)
    {
        Id = id;
        RoomId = roomId;
        PresidentUserId = presidentUserId;
        Name = name;
        ParticipantId = participantId;
        Budget = new Money(initialBudget);

        AddDomainEvent(new ClubInitializedEvent(Id, RoomId, PresidentUserId, Name, initialBudget, ParticipantId));
    }

    public void AddPlayerToRoster(Guid playerId, string name, PlayerPosition position, int overall, int age, decimal marketValue, Guid pickAttemptId)
    {
        if (_roster.Any(p => p.Id == playerId))
        {
            // Idempotency: if player is already added, emit success event again silently 
            // so the Saga doesn't get stuck or trigger a false compensating action.
            AddDomainEvent(new PlayerAddedToRosterEvent(Id, RoomId, playerId, overall, pickAttemptId));
            return;
        }

        if (_roster.Count >= MaxRosterSize)
        {
            AddDomainEvent(new PlayerRosterAdditionFailedEvent(Id, playerId, pickAttemptId, $"Roster is full. Max capacity is {MaxRosterSize}."));
            return;
        }

        var player = new Player(playerId, name, position, overall, age, marketValue);
        _roster.Add(player);

        AddDomainEvent(new PlayerAddedToRosterEvent(Id, RoomId, playerId, overall, pickAttemptId));
    }

    public void RemovePlayerFromRoster(Guid playerId)
    {
        var player = _roster.FirstOrDefault(p => p.Id == playerId);
        
        if (player == null)
        {
            // Idempotency: if player is not in the roster, do nothing
            return;
        }

        _roster.Remove(player);
        AddDomainEvent(new PlayerRemovedFromRosterEvent(Id, playerId));
    }

    public void MakeWeeklyDecision(int week, WeeklyDecisionType type)
    {
        if (_weeklyDecisions.Any(d => d.Week == week && d.Type == type))
        {
            AddDomainEvent(new WeeklyDecisionRejectedEvent(Id, week, type, "Decision has already been made for this week."));
            return;
        }

        var costAmount = GetDecisionCost(type);
        var cost = new Money(costAmount);

        if (!Budget.CanSubtract(cost))
        {
            AddDomainEvent(new WeeklyDecisionRejectedEvent(Id, week, type, "Insufficient funds."));
            return;
        }

        Budget = Budget.Subtract(cost);

        var decision = new WeeklyDecision(week, type, cost);
        _weeklyDecisions.Add(decision);

        AddDomainEvent(new BudgetDebitedEvent(Id, costAmount, $"Weekly Decision: {type} for Week {week}"));
        AddDomainEvent(new WeeklyDecisionMadeEvent(Id, week, type, costAmount));
    }

    public void ReceiveSponsorship(decimal amount)
    {
        var sponsorshipMoney = new Money(amount);
        Budget = Budget.Add(sponsorshipMoney);

        AddDomainEvent(new BudgetCreditedEvent(Id, amount, "Sponsorship Income"));
    }

    public void UpdateLineup(string lineupJson)
    {
        LineupJson = lineupJson;
    }

    public void UpdateFormation(string formation)
    {
        // Gecerli formasyon kodlarinin tam listesi frontend'de tanimli (bkz.
        // FORMATIONS sabiti, SeasonDashboard.tsx) — burada sadece bos deger
        // reddediliyor, tam validasyon frontend'in sundugu sabit secenek
        // listesiyle zaten garanti altinda.
        if (string.IsNullOrWhiteSpace(formation))
            throw new InvalidOperationException("Formation bos olamaz.");

        Formation = formation;
        // Formasyon degisince eski lineup'in slot ID'leri (orn. eski formasyonun
        // "CM1"i yeni formasyonda yok) artik anlamli olmayabilir — lineup'i
        // temizliyoruz, kullanici yeni formasyona oyuncularini yeniden diziyor.
        LineupJson = "{}";
    }

    private static decimal GetDecisionCost(WeeklyDecisionType type)
    {
        return type switch
        {
            WeeklyDecisionType.HireCoach => 500_000m,
            WeeklyDecisionType.StadiumInvestment => 2_000_000m,
            WeeklyDecisionType.MoraleBonus => 100_000m,
            _ => 0m
        };
    }
}
