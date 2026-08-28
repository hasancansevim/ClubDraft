using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerClaimedEvent
{
    Guid PickAttemptId { get; }
    Guid DraftSessionId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    int PickNumber { get; }
    string Name { get; }
    PlayerPosition Position { get; }
    int Overall { get; }
    int Age { get; }
    decimal MarketValue { get; }
    DateTime OccurredOn { get; }
}
