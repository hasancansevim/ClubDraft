namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IPlayerAddedToRosterEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    int Overall { get; }
}

public interface IPlayerRemovedFromRosterEvent
{
    Guid ClubId { get; }
    Guid PlayerId { get; }
}

public interface IPlayerRosterAdditionFailedEvent
{
    Guid PickAttemptId { get; }
    Guid ClubId { get; }
    Guid PlayerId { get; }
    string Reason { get; }
}

public interface IWeeklyDecisionMadeEvent
{
    Guid ClubId { get; }
    int Week { get; }
    int Type { get; } // Enum as int
    decimal Cost { get; }
}

public interface IClubInitializedEvent
{
    Guid ParticipantId { get; }
    Guid ClubId { get; }
    Guid RoomId { get; }
}
