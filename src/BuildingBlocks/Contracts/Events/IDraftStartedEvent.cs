namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IDraftStartedEvent
{
    Guid DraftSessionId { get; }
    DateTime OccurredOn { get; }
}
