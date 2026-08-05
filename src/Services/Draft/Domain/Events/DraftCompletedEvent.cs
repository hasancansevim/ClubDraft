using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Contracts.Events;

namespace ClubCraft.Draft.Domain.Events;

public class DraftCompletedEvent : IDomainEvent, IDraftCompletedEvent
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
