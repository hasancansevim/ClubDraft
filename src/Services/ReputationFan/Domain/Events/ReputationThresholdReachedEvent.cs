using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.ReputationFan.Domain.Events;

public record ReputationThresholdReachedEvent(Guid ClubId, int Threshold, int CurrentScore) : IDomainEvent, IReputationThresholdReachedEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
