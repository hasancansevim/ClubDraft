namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IDraftCompletedEvent
{
    Guid DraftSessionId { get; }
    DateTime OccurredOn { get; }
}
