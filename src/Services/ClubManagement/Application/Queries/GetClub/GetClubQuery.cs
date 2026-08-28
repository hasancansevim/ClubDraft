using MediatR;
using ClubCraft.ClubManagement.Domain.Enums;
using System.Collections.Generic;

namespace ClubCraft.ClubManagement.Application.Queries.GetClub;

public class GetClubQuery : IRequest<GetClubQueryResult?>
{
    public Guid ClubId { get; set; }
}

public class GetClubQueryResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public List<RosterPlayerDto> Roster { get; set; } = new();
    public List<WeeklyDecisionDto> WeeklyDecisions { get; set; } = new();
    public string LineupJson { get; set; } = "{}";
}

public class RosterPlayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public int Overall { get; set; }
    public int Age { get; set; }
    public decimal MarketValue { get; set; }
}

public class WeeklyDecisionDto
{
    public int Week { get; set; }
    public WeeklyDecisionType Type { get; set; }
    public decimal Cost { get; set; }
}
