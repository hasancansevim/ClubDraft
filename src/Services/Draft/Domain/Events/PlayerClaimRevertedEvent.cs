using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class PlayerClaimRevertedEvent : IDomainEvent
{
    public Guid PickAttemptId { get; }
    public Guid DraftSessionId { get; }
    public Guid PlayerId { get; }
    public Guid AffectedClubId { get; }
    public DateTime OccurredOn { get; }

    public PlayerClaimRevertedEvent(Guid pickAttemptId, Guid draftSessionId, Guid playerId, Guid affectedClubId)
    {
        PickAttemptId = pickAttemptId;
        DraftSessionId = draftSessionId;
        PlayerId = playerId;
        AffectedClubId = affectedClubId;
        OccurredOn = DateTime.UtcNow;
    }
}
