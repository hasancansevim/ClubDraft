using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class DraftStartedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public DateTime OccurredOn { get; }

    public DraftStartedEvent(Guid draftSessionId)
    {
        DraftSessionId = draftSessionId;
        OccurredOn = DateTime.UtcNow;
    }
}
