using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class DraftTurnAdvancedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public Guid NextClubId { get; }
    public int PickIndex { get; }
    public DateTime OccurredOn { get; }

    public DraftTurnAdvancedEvent(Guid draftSessionId, Guid nextClubId, int pickIndex)
    {
        DraftSessionId = draftSessionId;
        NextClubId = nextClubId;
        PickIndex = pickIndex;
        OccurredOn = DateTime.UtcNow;
    }
}
