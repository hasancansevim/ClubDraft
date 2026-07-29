namespace ClubCraft.BuildingBlocks.Common.SeedWork;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
