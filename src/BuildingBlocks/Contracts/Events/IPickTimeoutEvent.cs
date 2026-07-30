namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPickTimeoutEvent
{
    Guid PickAttemptId { get; }
}
