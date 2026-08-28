using MediatR;
using ClubCraft.MatchEngine.Application.Repositories;

namespace ClubCraft.MatchEngine.Application.Queries.GetStandings;

public class GetStandingsQueryHandler : IRequestHandler<GetStandingsQuery, List<TeamStandingDto>>
{
    private readonly IClubPowerRatingRepository _powerRatingRepository;
    private readonly IFixtureRepository _fixtureRepository;

    public GetStandingsQueryHandler(IClubPowerRatingRepository powerRatingRepository, IFixtureRepository fixtureRepository)
    {
        _powerRatingRepository = powerRatingRepository;
        _fixtureRepository = fixtureRepository;
    }

    public async Task<List<TeamStandingDto>> Handle(GetStandingsQuery request, CancellationToken cancellationToken)
    {
        // Get all clubs in the room from ClubPowerRatings
        var clubs = await _powerRatingRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);

        var standings = clubs.Select(c => new TeamStandingDto
        {
            ClubId = c.ClubId,
            Played = 0,
            Won = 0,
            Drawn = 0,
            Lost = 0,
            GoalsFor = 0,
            GoalsAgainst = 0,
            Points = 0
        }).ToDictionary(s => s.ClubId);

        // Fetch matches for this room that have been played
        var fixture = await _fixtureRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);
        var matches = fixture?.Matches.Where(m => m.IsPlayed).ToList() ?? new List<Domain.Entities.Match>();

        // Calculate points and goals
        foreach (var match in matches)
        {
            if (!standings.ContainsKey(match.HomeClubId)) continue;
            if (!standings.ContainsKey(match.AwayClubId)) continue;

            var home = standings[match.HomeClubId];
            var away = standings[match.AwayClubId];

            home.Played++;
            away.Played++;

            home.GoalsFor += match.HomeScore;
            home.GoalsAgainst += match.AwayScore;
            away.GoalsFor += match.AwayScore;
            away.GoalsAgainst += match.HomeScore;

            if (match.HomeScore > match.AwayScore)
            {
                home.Won++;
                home.Points += 3;
                away.Lost++;
            }
            else if (match.HomeScore < match.AwayScore)
            {
                away.Won++;
                away.Points += 3;
                home.Lost++;
            }
            else
            {
                home.Drawn++;
                home.Points += 1;
                away.Drawn++;
                away.Points += 1;
            }
        }

        // Sort by Points DESC, GoalDifference DESC, GoalsFor DESC
        return standings.Values
            .OrderByDescending(s => s.Points)
            .ThenByDescending(s => s.GoalDifference)
            .ThenByDescending(s => s.GoalsFor)
            .ToList();
    }
}
