using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.ClubManagement.Application.Commands.AddPlayerToRoster;

// Note: No specific Result type returned because this is invoked via Saga
// and communicates its result via Outbox domain events.
public record AddPlayerToRosterCommand(Guid ClubId, Guid PlayerId, string Name, PlayerPosition Position, int Overall, int Age, decimal MarketValue, Guid PickAttemptId) : IRequest;

public class AddPlayerToRosterCommandHandler : IRequestHandler<AddPlayerToRosterCommand>
{
    private readonly IClubRepository _repository;

    public AddPlayerToRosterCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AddPlayerToRosterCommand request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null)
            throw new InvalidOperationException($"Club with ID {request.ClubId} not found.");

        club.AddPlayerToRoster(request.PlayerId, request.Name, request.Position, request.Overall, request.Age, request.MarketValue, request.PickAttemptId);
        
        await _repository.SaveAsync(club, cancellationToken);
    }
}
