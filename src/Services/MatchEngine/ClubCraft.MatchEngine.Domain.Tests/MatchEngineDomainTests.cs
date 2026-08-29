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

        // Jenerator artik TEK bir devre (teams.Count - 1 hafta) uretmiyor -- 2026-08-28'de
        // "Bug A" duzeltmesiyle (bkz. spec.md) SeasonLengthWeeks'e (14) kadar bu devreyi
        // tekrarliyor, cunku kucuk ligler (2-6 takim) tek devreyle 10-14 haftalik sezona hic
        // ulasamiyordu. 5 takim + 1 "bye" slotuyla (Guid.Empty) 6'ya tamamlaniyor, her hafta
        // 3 eslesmeden biri hep bye oldugu icin gercekte 2 mac/hafta yayinlaniyor.
        int matchesPerWeek = ((clubIds.Count + 1) / 2) - 1; // bye ciftini cikar
        int totalExpectedMatches = RoundRobinFixtureGenerator.SeasonLengthWeeks * matchesPerWeek;

        var matchCounts = new Dictionary<Guid, int>();
        foreach (var id in clubIds) matchCounts[id] = 0;

        foreach (var match in fixture.Matches)
        {
            _output.WriteLine($"Week {match.Week}: {match.HomeClubId} vs {match.AwayClubId}");
            matchCounts[match.HomeClubId]++;
            matchCounts[match.AwayClubId]++;
        }

        Assert.Equal(totalExpectedMatches, fixture.Matches.Count);

        // 14 hafta, 5 haftalik devre uzunluguna (teams.Count) tam bolunmuyor (14 = 2 tam
        // devre + 4 fazla hafta) -- bu yuzden takimlar artik eskisi gibi BIREBIR esit sayida
        // mac oynamiyor: her takim ya 2 tam devrenin verdigi 8 macin +3'unu (kendi bye haftasi
        // fazla 4 haftanin icine denk gelen takimlar) ya da +4'unu (bye haftasi fazla 4
        // haftanin disinda kalan tek takim) alir. Toplam mac sayisi ve takim basina dusen
        // maclarin toplami yine de kesin ve dogrulanabilir.
        Assert.Equal(2 * totalExpectedMatches, matchCounts.Values.Sum());
        foreach (var count in matchCounts.Values)
        {
            Assert.InRange(count, 11, 12);
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
