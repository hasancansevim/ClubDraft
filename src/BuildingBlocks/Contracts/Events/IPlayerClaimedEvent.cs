namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerClaimedEvent
{
    Guid DraftSessionId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    int PickNumber { get; }
    DateTime OccurredOn { get; }
}
