using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Events;

public class PlayerClaimedEvent : IDomainEvent
{
    public Guid PickAttemptId { get; }
    public Guid DraftSessionId { get; }
    public Guid ClubId { get; }
    public Guid PlayerId { get; }
    public int PickNumber { get; }
    public string Name { get; }
    public string Position { get; }
    public int Overall { get; }
    public int Age { get; }
    public decimal MarketValue { get; }
    public DateTime OccurredOn { get; }

    public PlayerClaimedEvent(Guid pickAttemptId, Guid draftSessionId, Guid clubId, Guid playerId, int pickNumber, string name, string position, int overall, int age, decimal marketValue)
    {
        PickAttemptId = pickAttemptId;
        DraftSessionId = draftSessionId;
        ClubId = clubId;
        PlayerId = playerId;
        PickNumber = pickNumber;
        Name = name;
        Position = position;
        Overall = overall;
        Age = age;
        MarketValue = marketValue;
        OccurredOn = DateTime.UtcNow;
    }
}
