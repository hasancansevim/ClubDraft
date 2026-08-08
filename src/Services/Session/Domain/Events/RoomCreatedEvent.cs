using System;
using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Session.Domain.Events
{
    public class RoomCreatedEvent : IDomainEvent, IRoomCreatedEvent
    {
        public Guid RoomId { get; }
        public string HostUserId { get; }
        public DateTime OccurredOn { get; }

        public RoomCreatedEvent(Guid roomId, string hostUserId)
        {
            RoomId = roomId;
            HostUserId = hostUserId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
