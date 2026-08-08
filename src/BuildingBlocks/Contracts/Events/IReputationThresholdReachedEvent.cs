namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IReputationThresholdReachedEvent
{
    Guid ClubId { get; }
    int Threshold { get; }
}
