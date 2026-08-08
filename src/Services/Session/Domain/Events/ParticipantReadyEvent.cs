using System;
using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Session.Domain.Events;

public class ParticipantReadyEvent : IDomainEvent, IParticipantReadyEvent
{
    public Guid RoomId { get; }
    public Guid ParticipantId { get; }
    public string Phase { get; }
    public DateTime OccurredOn { get; }

    public ParticipantReadyEvent(Guid roomId, Guid participantId, string phase)
    {
        RoomId = roomId;
        ParticipantId = participantId;
        Phase = phase;
        OccurredOn = DateTime.UtcNow;
    }
}
