using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class PlayerClaimRevertedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public Guid PlayerId { get; }
    public Guid AffectedClubId { get; }
    public DateTime OccurredOn { get; }

    public PlayerClaimRevertedEvent(Guid draftSessionId, Guid playerId, Guid affectedClubId)
    {
        DraftSessionId = draftSessionId;
        PlayerId = playerId;
        AffectedClubId = affectedClubId;
        OccurredOn = DateTime.UtcNow;
    }
}
