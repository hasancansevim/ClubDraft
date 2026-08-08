using System;
using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Session.Domain.Events
{
    public class ParticipantJoinedEvent : IDomainEvent, IParticipantJoinedEvent
    {
        public Guid RoomId { get; }
        public Guid ParticipantId { get; }
        public string UserId { get; }
        public string ClubName { get; }
        public DateTime OccurredOn { get; }

        public ParticipantJoinedEvent(Guid roomId, Guid participantId, string userId, string clubName)
        {
            RoomId = roomId;
            ParticipantId = participantId;
            UserId = userId;
            ClubName = clubName;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
