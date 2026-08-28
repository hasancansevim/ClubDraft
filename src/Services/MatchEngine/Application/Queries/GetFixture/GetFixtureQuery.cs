using MediatR;

namespace ClubCraft.MatchEngine.Application.Queries.GetFixture;

public class GetFixtureQuery : IRequest<List<MatchDto>>
{
    public Guid RoomId { get; set; }
}

public class MatchDto
{
    public Guid Id { get; set; }
    public int Week { get; set; }
    public Guid HomeClubId { get; set; }
    public Guid AwayClubId { get; set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public bool IsPlayed { get; set; }
}
