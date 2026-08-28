using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.BuildingBlocks.Contracts.Commands;

public interface IAddPlayerToRosterCommand
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    string Name { get; }
    PlayerPosition Position { get; }
    int Overall { get; }
    int Age { get; }
    decimal MarketValue { get; }
}
