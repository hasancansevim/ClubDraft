using ClubCraft.MatchEngine.Domain.Entities;
using ClubCraft.MatchEngine.Domain.Services;
using Xunit;
using Xunit.Abstractions;

namespace ClubCraft.MatchEngine.Domain.Tests;

public class MatchEngineDomainTests
{
    private readonly ITestOutputHelper _output;

    public MatchEngineDomainTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateFixture_OddNumberOfTeams_IncludesByes()
    {
        var generator = new RoundRobinFixtureGenerator();
        var clubIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }; // 5 teams
        
        var fixture = generator.GenerateFixture(Guid.NewGuid(), clubIds);

        _output.WriteLine("--- 5 Teams Fixture ---");
        int totalExpectedMatches = (5 * 4) / 2; // Every team plays 4 matches
        
        // Let's verify every team plays everyone exactly once
        var matchCounts = new Dictionary<Guid, int>();
        foreach (var id in clubIds) matchCounts[id] = 0;

        foreach (var match in fixture.Matches)
        {
            _output.WriteLine($"Week {match.Week}: {match.HomeClubId} vs {match.AwayClubId}");
            matchCounts[match.HomeClubId]++;
            matchCounts[match.AwayClubId]++;
        }

        Assert.Equal(totalExpectedMatches, fixture.Matches.Count);
        
        // Every team should have played exactly 4 times (since there are 5 teams)
        foreach (var count in matchCounts.Values)
        {
            Assert.Equal(4, count);
        }
    }

    [Fact]
    public void MatchSimulator_WeightedRandom_StrongerTeamWinsMoreOften()
    {
        var simulator = new MatchSimulator(seed: 42); // Fixed seed for reproducible tests but randomized over iteration
        
        Guid strongTeamId = Guid.NewGuid();
        Guid weakTeamId = Guid.NewGuid();

        int strongTeamPower = 90;
        int weakTeamPower = 50;

        int strongWins = 0;
        int weakWins = 0;
        int draws = 0;

        int iterations = 1000;

        for (int i = 0; i < iterations; i++)
        {
            var match = new Match(Guid.NewGuid(), 1, strongTeamId, weakTeamId);
            
            // Swap home/away randomly to eliminate home advantage bias in stats
            if (i % 2 == 0)
            {
                simulator.Simulate(match, strongTeamPower, weakTeamPower);
                if (match.HomeScore > match.AwayScore) strongWins++;
                else if (match.AwayScore > match.HomeScore) weakWins++;
                else draws++;
            }
            else
            {
                simulator.Simulate(match, weakTeamPower, strongTeamPower);
                if (match.AwayScore > match.HomeScore) strongWins++; // strong team is away
                else if (match.HomeScore > match.AwayScore) weakWins++;
                else draws++;
            }
        }

        _output.WriteLine("--- Simulation Results over 1000 Matches ---");
        _output.WriteLine($"Strong Team (90 Power) Wins: {strongWins} ({(double)strongWins / iterations * 100}%)");
        _output.WriteLine($"Weak Team (50 Power) Wins: {weakWins} ({(double)weakWins / iterations * 100}%)");
        _output.WriteLine($"Draws: {draws} ({(double)draws / iterations * 100}%)");

        // Statistically, the strong team should win significantly more than the weak team
        Assert.True(strongWins > weakWins * 1.5, "Strong team didn't win significantly more than weak team.");
    }
}
