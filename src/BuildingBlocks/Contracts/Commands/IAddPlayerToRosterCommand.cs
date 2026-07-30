namespace ClubCraft.BuildingBlocks.Contracts.Commands;

public interface IAddPlayerToRosterCommand
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    string Name { get; }
    string Position { get; }
    int Overall { get; }
    int Age { get; }
    decimal MarketValue { get; }
}
