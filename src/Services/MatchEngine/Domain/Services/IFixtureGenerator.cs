using ClubCraft.MatchEngine.Domain.Aggregates;

namespace ClubCraft.MatchEngine.Domain.Services;

public interface IFixtureGenerator
{
    Fixture GenerateFixture(Guid roomId, List<Guid> clubIds);
}
