using ClubCraft.MatchEngine.Domain.Aggregates;
using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

public class RoundRobinFixtureGenerator : IFixtureGenerator
{
    // Sezon uzunluğu (bkz. spec.md §2: "Sezon Fazı — 10-14 hafta sürer", frontend'de de
    // sabit 14 olarak gösteriliyor). Tek bir round-robin turu (teams.Count - 1 hafta) küçük
    // lig boylarında (2-6 takım) bu süreye asla ulaşmıyordu — round tamamlanınca fikstürde
    // o haftalar için hiç Match satırı kalmıyor, SimulateMatchesForWeek de sessizce hiçbir
    // şey yapmadan "hafta tamamlandı" event'i yayınlıyordu (bkz. spec.md, 2026-08-28 notu).
    public const int SeasonLengthWeeks = 14;

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

        int matchesPerWeek = teams.Count / 2;
        var matches = new List<Match>();

        // Devre-döngüsü (circle method) periyodu teams.Count - 1'dir; bu döngüyü sezon
        // uzunluğuna kadar tekrar ederek (double/triple round-robin gibi) her haftanın
        // gerçek maçları olmasını sağlıyoruz.
        for (int week = 1; week <= SeasonLengthWeeks; week++)
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
