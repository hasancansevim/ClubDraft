using MediatR;
using ClubCraft.Draft.Application.Repositories;
using ClubCraft.Draft.Domain.Events;

namespace ClubCraft.Draft.Application.Commands.ClaimPlayer;

public class ClaimPlayerCommandHandler : IRequestHandler<ClaimPlayerCommand, ClaimPlayerResult>
{
    private readonly IDraftSessionRepository _repository;

    public ClaimPlayerCommandHandler(IDraftSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClaimPlayerResult> Handle(ClaimPlayerCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByIdAsync(request.DraftSessionId, cancellationToken);
        
        if (session == null)
            throw new InvalidOperationException($"DraftSession with ID {request.DraftSessionId} not found.");

        var eventCountBefore = session.DomainEvents.Count;
        session.ClaimPlayer(request.ClubId, request.PlayerId);

        var newEvents = session.DomainEvents.Skip(eventCountBefore);
        var rejection = newEvents.OfType<PlayerClaimRejectedEvent>().FirstOrDefault();

        if (rejection != null)
        {
            await _repository.SaveAsync(session, cancellationToken);
            return ClaimPlayerResult.Fail(rejection.Reason);
        }

        var claimed = newEvents.OfType<PlayerClaimedEvent>().FirstOrDefault();
        await _repository.SaveAsync(session, cancellationToken);
        
        return ClaimPlayerResult.IsSuccess(claimed!.PickNumber);
    }
}

