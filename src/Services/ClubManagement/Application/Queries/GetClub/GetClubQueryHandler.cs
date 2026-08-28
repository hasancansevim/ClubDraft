using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;

namespace ClubCraft.ClubManagement.Application.Queries.GetClub;

public class GetClubQueryHandler : IRequestHandler<GetClubQuery, GetClubQueryResult?>
{
    private readonly IClubRepository _repository;

    public GetClubQueryHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetClubQueryResult?> Handle(GetClubQuery request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null)
            return null;

        return new GetClubQueryResult
        {
            Id = club.Id,
            Name = club.Name,
            Budget = club.Budget.Amount,
            LineupJson = club.LineupJson,
            Formation = club.Formation,
            Roster = club.Roster.Select(p => new RosterPlayerDto
            {
                Id = p.Id,
                Name = p.Name,
                Position = p.Position,
                Overall = p.Overall,
                Age = p.Age,
                MarketValue = p.MarketValue
            }).ToList(),
            WeeklyDecisions = club.WeeklyDecisions.Select(d => new WeeklyDecisionDto
            {
                Week = d.Week,
                Type = d.Type,
                Cost = d.Cost.Amount
            }).ToList()
        };
    }
}
