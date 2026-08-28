using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;

namespace ClubCraft.ClubManagement.Application.Commands.UpdateLineup;

public class UpdateLineupCommandHandler : IRequestHandler<UpdateLineupCommand, bool>
{
    private readonly IClubRepository _repository;

    public UpdateLineupCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateLineupCommand request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null) return false;

        club.UpdateLineup(request.LineupJson);
        await _repository.SaveAsync(club, cancellationToken);
        
        return true;
    }
}
