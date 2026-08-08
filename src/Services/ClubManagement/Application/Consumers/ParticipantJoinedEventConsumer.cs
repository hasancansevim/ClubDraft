using MassTransit;
using MediatR;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.ClubManagement.Application.Commands.InitializeClub;
using ClubCraft.ClubManagement.Application.Repositories;

namespace ClubCraft.ClubManagement.Application.Consumers;

public class ParticipantJoinedEventConsumer : IConsumer<IParticipantJoinedEvent>
{
    private readonly IMediator _mediator;
    private readonly IClubRepository _clubRepository;

    public ParticipantJoinedEventConsumer(IMediator mediator, IClubRepository clubRepository)
    {
        _mediator = mediator;
        _clubRepository = clubRepository;
    }

    public async Task Consume(ConsumeContext<IParticipantJoinedEvent> context)
    {
        // 1. Idempotency Check
        var existingClub = await _clubRepository.GetByParticipantIdAsync(context.Message.ParticipantId);
        if (existingClub != null)
        {
            // Already processed. Return silently.
            return;
        }

        // 2. UserId -> Guid TryParse
        if (!Guid.TryParse(context.Message.UserId, out var presidentUserId))
        {
            // If the UserId is not a valid Guid, generate a new one for fallback.
            presidentUserId = Guid.NewGuid();
        }

        var clubId = Guid.NewGuid();
        var command = new InitializeClubCommand(clubId, context.Message.RoomId, presidentUserId, context.Message.ClubName, context.Message.ParticipantId);
        
        await _mediator.Send(command);
    }
}
