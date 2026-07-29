using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class PlayerClaimRejectedEvent : IDomainEvent
{
    public Guid DraftSessionId { get; }
    public Guid ClubId { get; }
    public Guid PlayerId { get; }
    public string Reason { get; }
    public DateTime OccurredOn { get; }

    public PlayerClaimRejectedEvent(Guid draftSessionId, Guid clubId, Guid playerId, string reason)
    {
        DraftSessionId = draftSessionId;
        ClubId = clubId;
        PlayerId = playerId;
        Reason = reason;
        OccurredOn = DateTime.UtcNow;
    }
}
