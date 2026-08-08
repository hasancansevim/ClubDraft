using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Session.Domain.Events
{
    public class AllParticipantsReadyForNextWeekEvent : IDomainEvent, IAllParticipantsReadyForNextWeekEvent
    {
        public Guid RoomId { get; }
        public int Week { get; }
        public DateTime OccurredOn { get; }

        public AllParticipantsReadyForNextWeekEvent(Guid roomId, int week)
        {
            RoomId = roomId;
            Week = week;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
