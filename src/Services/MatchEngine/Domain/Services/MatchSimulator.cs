using ClubCraft.MatchEngine.Domain.Entities;
using ClubCraft.MatchEngine.Domain.Enums;
using ClubCraft.MatchEngine.Domain.ValueObjects;

namespace ClubCraft.MatchEngine.Domain.Services;

public class MatchSimulator : IMatchSimulator
{
    private readonly Random _random;

    public MatchSimulator()
    {
        _random = new Random();
    }

    public MatchSimulator(int seed)
    {
        _random = new Random(seed);
    }

    public void Simulate(Match match, double homePower, double awayPower)
    {
        // 1. Add Home Advantage (+5% relative to the total)
        // A simple way is to add a small flat bonus to home power
        homePower += homePower * 0.05;

        // 2. Calculate Win Probabilities
        double totalPower = homePower + awayPower;

        // If both have 0 power for some reason, equal chance
        if (totalPower <= 0)
        {
            homePower = 100;
            awayPower = 100;
            totalPower = 200;
        }

        double homeWinProb = homePower / totalPower;
        double awayWinProb = awayPower / totalPower;

        // 3. Adjust for Draw Probability (Base 20%)
        double drawProb = 0.20;
        
        // Scale down win probabilities
        homeWinProb = homeWinProb * (1 - drawProb);
        awayWinProb = awayWinProb * (1 - drawProb);

        // Calculate thresholds
        double homeThreshold = homeWinProb;
        double drawThreshold = homeThreshold + drawProb;

        // 4. Roll the dice
        double roll = _random.NextDouble();
        int homeScore = 0;
        int awayScore = 0;

        if (roll < homeThreshold)
        {
            // Home Wins
            (homeScore, awayScore) = GetRandomScore(true);
        }
        else if (roll < drawThreshold)
        {
            // Draw
            int score = _random.Next(0, 3); // 0-0, 1-1, 2-2
            homeScore = score;
            awayScore = score;
        }
        else
        {
            // Away Wins
            (homeScore, awayScore) = GetRandomScore(false);
        }

        // 5. Generate Events based on score
        var events = GenerateMatchEvents(match.HomeClubId, match.AwayClubId, homeScore, awayScore);

        match.Resolve(homeScore, awayScore, events);
    }

    private (int WinnerScore, int LoserScore) GetRandomScore(bool homeWins)
    {
        // Common football winning scores: 1-0, 2-0, 2-1, 3-0, 3-1, 3-2, 4-0
        var possibleScores = new List<(int w, int l)>
        {
            (1, 0), (1, 0), (1, 0), // 30%
            (2, 0), (2, 0),         // 20%
            (2, 1), (2, 1),         // 20%
            (3, 0), (3, 1),         // 20%
            (3, 2), (4, 0)          // 10%
        };

        var score = possibleScores[_random.Next(possibleScores.Count)];

        if (homeWins)
        {
            return (score.w, score.l);
        }
        else
        {
            return (score.l, score.w);
        }
    }

    private IEnumerable<MatchEvent> GenerateMatchEvents(Guid homeClubId, Guid awayClubId, int homeScore, int awayScore)
    {
        var events = new List<MatchEvent>();
        
        for (int i = 0; i < homeScore; i++)
        {
            events.Add(new MatchEvent(_random.Next(1, 91), MatchEventType.Goal, homeClubId));
        }

        for (int i = 0; i < awayScore; i++)
        {
            events.Add(new MatchEvent(_random.Next(1, 91), MatchEventType.Goal, awayClubId));
        }

        return events.OrderBy(e => e.Minute);
    }
}
