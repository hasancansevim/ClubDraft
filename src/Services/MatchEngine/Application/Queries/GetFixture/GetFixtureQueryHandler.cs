using MediatR;
using ClubCraft.MatchEngine.Application.Repositories;

namespace ClubCraft.MatchEngine.Application.Queries.GetFixture;

public class GetFixtureQueryHandler : IRequestHandler<GetFixtureQuery, List<MatchDto>>
{
    private readonly IFixtureRepository _fixtureRepository;

    public GetFixtureQueryHandler(IFixtureRepository fixtureRepository)
    {
        _fixtureRepository = fixtureRepository;
    }

    public async Task<List<MatchDto>> Handle(GetFixtureQuery request, CancellationToken cancellationToken)
    {
        var fixture = await _fixtureRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);
        if (fixture == null) return new List<MatchDto>();

        return fixture.Matches
            .OrderBy(m => m.Week)
            .Select(m => new MatchDto
            {
                Id = m.Id,
                Week = m.Week,
                HomeClubId = m.HomeClubId,
                AwayClubId = m.AwayClubId,
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                IsPlayed = m.IsPlayed
            })
            .ToList();
    }
}
