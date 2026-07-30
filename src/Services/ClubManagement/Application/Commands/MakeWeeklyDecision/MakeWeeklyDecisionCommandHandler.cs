using MediatR;
using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.ClubManagement.Domain.Events;

namespace ClubCraft.ClubManagement.Application.Commands.MakeWeeklyDecision;

public class MakeWeeklyDecisionCommandHandler : IRequestHandler<MakeWeeklyDecisionCommand, MakeWeeklyDecisionResult>
{
    private readonly IClubRepository _repository;

    public MakeWeeklyDecisionCommandHandler(IClubRepository repository)
    {
        _repository = repository;
    }

    public async Task<MakeWeeklyDecisionResult> Handle(MakeWeeklyDecisionCommand request, CancellationToken cancellationToken)
    {
        var club = await _repository.GetByIdAsync(request.ClubId, cancellationToken);
        if (club == null)
            throw new InvalidOperationException($"Club with ID {request.ClubId} not found.");

        var eventCountBefore = club.DomainEvents.Count;
        
        club.MakeWeeklyDecision(request.Week, request.Type);

        var newEvents = club.DomainEvents.Skip(eventCountBefore);
        var rejection = newEvents.OfType<WeeklyDecisionRejectedEvent>().FirstOrDefault();

        if (rejection != null)
        {
            await _repository.SaveAsync(club, cancellationToken);
            return MakeWeeklyDecisionResult.Fail(rejection.Reason);
        }

        var successEvent = newEvents.OfType<WeeklyDecisionMadeEvent>().FirstOrDefault();
        await _repository.SaveAsync(club, cancellationToken);

        return MakeWeeklyDecisionResult.IsSuccess(successEvent!.Cost);
    }
}
