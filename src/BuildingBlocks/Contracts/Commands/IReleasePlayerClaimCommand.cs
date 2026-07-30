namespace ClubCraft.BuildingBlocks.Contracts.Commands;

public interface IReleasePlayerClaimCommand
{
    Guid PickAttemptId { get; }
    Guid DraftSessionId { get; }
    Guid PlayerId { get; }
}
