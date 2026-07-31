using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class DraftCompletedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public IEnumerable<Guid> ClubIds { get; }
    public DateTime OccurredOn { get; }

    public DraftCompletedEvent(Guid draftSessionId, IEnumerable<Guid> clubIds)
    {
        DraftSessionId = draftSessionId;
        ClubIds = clubIds;
        OccurredOn = DateTime.UtcNow;
    }
}
