using MediatR;
using System.Collections.Generic;

namespace ClubCraft.MatchEngine.Application.Queries.GetStandings;

public class GetStandingsQuery : IRequest<List<TeamStandingDto>>
{
    public Guid RoomId { get; set; }
}

public class TeamStandingDto
{
    public Guid ClubId { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
    public int Points { get; set; }
}
