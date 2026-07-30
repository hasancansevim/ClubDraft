using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;

namespace ClubCraft.ClubManagement.Application.Commands.ReleasePlayerFromRoster;

public record ReleasePlayerFromRosterCommand(Guid ClubId, Guid PlayerId, Guid PickAttemptId) : IRequest;

public class ReleasePlayerFromRosterCommandHandler : IRequestHandler<ReleasePlayerFromRosterCommand>
{
    private readonly IClubRepository _repository;

    public ReleasePlayerFromRosterCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ReleasePlayerFromRosterCommand request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null)
            throw new InvalidOperationException($"Club with ID {request.ClubId} not found.");

        club.RemovePlayerFromRoster(request.PlayerId);
        
        await _repository.SaveAsync(club, cancellationToken);
    }
}
