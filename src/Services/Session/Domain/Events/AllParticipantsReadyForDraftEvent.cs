using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Session.Domain.Events
{
    public class AllParticipantsReadyForDraftEvent : IDomainEvent, IAllParticipantsReadyForDraftEvent
    {
        public Guid RoomId { get; }
        public IEnumerable<Guid> ParticipantClubIds { get; }
        public DateTime OccurredOn { get; }

        public AllParticipantsReadyForDraftEvent(Guid roomId, IEnumerable<Guid> participantClubIds)
        {
            RoomId = roomId;
            ParticipantClubIds = participantClubIds;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
