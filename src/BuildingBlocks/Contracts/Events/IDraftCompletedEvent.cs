namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IDraftCompletedEvent
{
    Guid DraftSessionId { get; }
    IEnumerable<Guid> ClubIds { get; }
    DateTime OccurredOn { get; }
}
