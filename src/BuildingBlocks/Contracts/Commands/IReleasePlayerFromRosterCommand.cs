namespace ClubCraft.BuildingBlocks.Contracts.Commands;

public interface IReleasePlayerFromRosterCommand
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
}
