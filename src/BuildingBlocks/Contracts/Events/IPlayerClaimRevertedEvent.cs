namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerClaimRevertedEvent
{
    Guid PickAttemptId { get; }
    Guid DraftSessionId { get; }
    Guid PlayerId { get; }
    Guid AffectedClubId { get; }
    DateTime OccurredOn { get; }
}
