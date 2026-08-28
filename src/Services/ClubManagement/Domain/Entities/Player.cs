using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.ClubManagement.Domain.Entities;

public class Player : Entity<Guid>
{
    public string Name { get; private set; }
    public PlayerPosition Position { get; private set; }
    public int Overall { get; private set; }
    public int Age { get; private set; }
    public decimal MarketValue { get; private set; }

    private Player() { } // EF Core

    public Player(Guid id, string name, PlayerPosition position, int overall, int age, decimal marketValue)
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
