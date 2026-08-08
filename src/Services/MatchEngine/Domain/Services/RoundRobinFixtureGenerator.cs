using ClubCraft.MatchEngine.Domain.Aggregates;
using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

public class RoundRobinFixtureGenerator : IFixtureGenerator
{
    public Fixture GenerateFixture(Guid roomId, List<Guid> clubIds)
    {
        if (clubIds == null || clubIds.Count < 2)
            throw new ArgumentException("At least 2 clubs are required to generate a fixture.");

        // If odd number of clubs, add a dummy club (Guid.Empty) to represent a "bye" week.
        bool hasBye = false;
        var teams = new List<Guid>(clubIds);
        if (teams.Count % 2 != 0)
        {
            teams.Add(Guid.Empty);
            hasBye = true;
        }

        int totalWeeks = teams.Count - 1;
        int matchesPerWeek = teams.Count / 2;
        var matches = new List<Match>();

        for (int week = 1; week <= totalWeeks; week++)
        {
            for (int i = 0; i < matchesPerWeek; i++)
            {
                var home = teams[i];
                var away = teams[teams.Count - 1 - i];

                // Swap home and away on alternate weeks to balance home/away games
                if (week % 2 == 0)
                {
                    var temp = home;
                    home = away;
                    away = temp;
                }

                // If either team is Guid.Empty, it means the other team has a bye this week.
                if (home != Guid.Empty && away != Guid.Empty)
                {
                    matches.Add(new Match(Guid.NewGuid(), week, home, away));
                }
            }

            // Rotate teams (except the first one)
            var lastTeam = teams.Last();
            teams.RemoveAt(teams.Count - 1);
            teams.Insert(1, lastTeam);
        }

        return new Fixture(Guid.NewGuid(), roomId, matches);
    }
}
