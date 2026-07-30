using MediatR;
using ClubCraft.Draft.Application.Repositories;

namespace ClubCraft.Draft.Application.Commands.ReleasePlayerClaim;

public class ReleasePlayerClaimCommandHandler : IRequestHandler<ReleasePlayerClaimCommand>
{
    private readonly IDraftSessionRepository _repository;

    public ReleasePlayerClaimCommandHandler(IDraftSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ReleasePlayerClaimCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.DraftSessionId, cancellationToken);
        if (session == null)
            throw new InvalidOperationException($"DraftSession with ID {request.DraftSessionId} not found.");

        session.RevertClaim(request.PickAttemptId, request.PlayerId);
        
        await _repository.SaveAsync(session, cancellationToken);
    }
}
