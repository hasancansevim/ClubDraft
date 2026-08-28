using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;

namespace ClubCraft.ClubManagement.Application.Commands.UpdateFormation;

public class UpdateFormationCommandHandler : IRequestHandler<UpdateFormationCommand, bool>
{
    private readonly IClubRepository _repository;

    public UpdateFormationCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateFormationCommand request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null) return false;

        club.UpdateFormation(request.Formation);
        await _repository.SaveAsync(club, cancellationToken);

        return true;
    }
}
