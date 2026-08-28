using MediatR;
using MediatR;
using ClubCraft.ReputationFan.Application.Repositories;

namespace ClubCraft.ReputationFan.Application.Queries.GetReputation;

public class GetReputationQueryHandler : IRequestHandler<GetReputationQuery, int>
{
    private readonly IClubReputationRepository _repository;

    public GetReputationQueryHandler(IClubReputationRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(GetReputationQuery request, CancellationToken cancellationToken)
    {
        var reputation = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (reputation == null) return 0;

        return reputation.Score;
    }
}
