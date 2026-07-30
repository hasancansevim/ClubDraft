using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.ClubManagement.Domain.Entities;

public class Player : Entity<Guid>
{
    public string Name { get; private set; }
    public string Position { get; private set; }
    public int Overall { get; private set; }
    public int Age { get; private set; }
    public decimal MarketValue { get; private set; }
    
    private Player() { } // EF Core

    public Player(Guid id, string name, string position, int overall, int age, decimal marketValue)
    {
        // By accepting id from outside, we can reuse the same Guid from Draft service (PlayerId)
        Id = id;
        Name = name;
        Position = position;
        Overall = overall;
        Age = age;
        MarketValue = marketValue;
    }
}
