using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class PlayerClaimedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public Guid ClubId { get; }
    public Guid PlayerId { get; }
    public int PickNumber { get; }
    public DateTime OccurredOn { get; }

    public PlayerClaimedEvent(Guid draftSessionId, Guid clubId, Guid playerId, int pickNumber)
    {
        DraftSessionId = draftSessionId;
        ClubId = clubId;
        PlayerId = playerId;
        PickNumber = pickNumber;
        OccurredOn = DateTime.UtcNow;
    }
}
