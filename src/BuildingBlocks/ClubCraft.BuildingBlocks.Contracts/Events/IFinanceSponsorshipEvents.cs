namespace ClubCraft.BuildingBlocks.Contracts.Events;

public interface ISponsorshipAcceptedEvent
{
    Guid ClubId { get; }
    decimal Amount { get; }
}
