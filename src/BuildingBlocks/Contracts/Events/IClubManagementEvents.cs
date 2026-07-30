namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerAddedToRosterEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
}

public interface IPlayerRosterAdditionFailedEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    string Reason { get; }
}
