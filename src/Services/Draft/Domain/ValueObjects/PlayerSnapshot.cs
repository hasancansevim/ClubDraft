using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.ValueObjects;

public class PlayerSnapshot : ValueObject
{
    public string Name { get; private set; }
    public string Position { get; private set; }
    public int Overall { get; private set; }
    public int Age { get; private set; }
    public decimal MarketValue { get; private set; }

    private PlayerSnapshot() { } // EF Core

    public PlayerSnapshot(string name, string position, int overall, int age, decimal marketValue)
    {
        Name = name;
        Position = position;
        Overall = overall;
        Age = age;
        MarketValue = marketValue;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Position;
        yield return Overall;
        yield return Age;
        yield return MarketValue;
    }
}
