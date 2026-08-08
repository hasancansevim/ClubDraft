namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IAllParticipantsReadyForNextWeekEvent
{
    Guid RoomId { get; }
    int Week { get; }
}

public interface IAllParticipantsReadyForDraftEvent
{
    Guid RoomId { get; }
    IEnumerable<Guid> ParticipantClubIds { get; }
}

public interface IRoomCreatedEvent
{
    Guid RoomId { get; }
    string HostUserId { get; }
}

public interface IParticipantJoinedEvent
{
    Guid RoomId { get; }
    Guid ParticipantId { get; }
    string UserId { get; }
    string ClubName { get; }
}

public interface IParticipantReadyEvent
{
    Guid RoomId { get; }
    Guid ParticipantId { get; }
    string Phase { get; }
}
