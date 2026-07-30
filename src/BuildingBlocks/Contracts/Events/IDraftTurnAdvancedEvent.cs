namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface IDraftTurnAdvancedEvent
{
    Guid DraftSessionId { get; }
    Guid NextClubId { get; }
    int PickIndex { get; }
    DateTime OccurredOn { get; }
}
