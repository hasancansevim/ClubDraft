namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IMatchSimulatedEvent
{
    Guid MatchId { get; }
    Guid RoomId { get; }
    int Week { get; }
    Guid HomeClubId { get; }
    Guid AwayClubId { get; }
    int HomeScore { get; }
    int AwayScore { get; }
}

public interface IWeekSimulationCompletedEvent
{
    Guid RoomId { get; }
    int Week { get; }
}
