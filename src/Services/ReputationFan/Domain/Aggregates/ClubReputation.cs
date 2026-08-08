using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.ReputationFan.Domain.Entities;
using ClubCraft.ReputationFan.Domain.Events;

namespace ClubCraft.ReputationFan.Domain.Aggregates;

public class ClubReputation : AggregateRoot<Guid>
{
    public int Score { get; private set; }
    public int LastReportedThreshold { get; private set; }
    
    private readonly List<ReputationChange> _history = new();
    public IReadOnlyCollection<ReputationChange> History => _history.AsReadOnly();

    private ClubReputation() { } // EF Core

    public ClubReputation(Guid clubId)
    {
        Id = clubId;
        Score = 0;
        LastReportedThreshold = 0;
    }

    public void AddReputation(int delta, string reason)
    {
        if (delta == 0) return;

        Score += delta;
        if (Score < 0) Score = 0;

        _history.Add(new ReputationChange(delta, reason, DateTime.UtcNow));

        CheckAndFireThresholdEvent();
    }

    private void CheckAndFireThresholdEvent()
    {
        // 50 puanda bir tetiklenir (50, 100, 150...)
        int currentThreshold = (Score / 50) * 50;

        if (currentThreshold > LastReportedThreshold && currentThreshold > 0)
        {
            AddDomainEvent(new ReputationThresholdReachedEvent(Id, currentThreshold, Score));
            LastReportedThreshold = currentThreshold;
        }
    }
}
