using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.ClubManagement.Domain.Aggregates;

namespace ClubCraft.ClubManagement.Application.Commands.InitializeClub;

public record InitializeClubCommand(Guid ClubId, Guid RoomId, Guid PresidentUserId, string Name, Guid ParticipantId) : IRequest;

public class InitializeClubCommandHandler : IRequestHandler<InitializeClubCommand>
{
    private readonly IClubRepository _repository;

    public InitializeClubCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(InitializeClubCommand request, CancellationToken cancellationToken)
    {
        var existingClub = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (existingClub != null)
            throw new InvalidOperationException($"Club with ID {request.ClubId} already exists.");

        var club = new Club(request.ClubId, request.RoomId, request.PresidentUserId, request.Name, Club.DefaultInitialBudget, request.ParticipantId);
        await _repository.SaveAsync(club, cancellationToken);
    }
}
