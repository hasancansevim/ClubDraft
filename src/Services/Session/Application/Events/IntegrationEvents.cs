using System;

namespace ClubCraft.Session.Application.Events
{
    public interface IDraftCompletedEvent
    {
        Guid GameRoomId { get; }
    }

    public interface IWeekSimulationCompletedEvent
    {
        Guid GameRoomId { get; }
    }
}
