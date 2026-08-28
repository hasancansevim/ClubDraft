using MediatR;
using ClubCraft.ReputationFan.Domain.Aggregates;

namespace ClubCraft.ReputationFan.Application.Queries.GetReputation;

public class GetReputationQuery : IRequest<int>
{
    public Guid ClubId { get; set; }
}
