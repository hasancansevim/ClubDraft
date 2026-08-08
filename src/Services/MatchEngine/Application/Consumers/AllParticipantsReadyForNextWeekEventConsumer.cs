using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Commands.SimulateMatchesForWeek;
using MassTransit;
using MediatR;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class AllParticipantsReadyForNextWeekEventConsumer : IConsumer<IAllParticipantsReadyForNextWeekEvent>
{
    private readonly IMediator _mediator;

    public AllParticipantsReadyForNextWeekEventConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IAllParticipantsReadyForNextWeekEvent> context)
    {
        var msg = context.Message;
        
        // Inbox pattern ensures idempotency.
        await _mediator.Send(new SimulateMatchesForWeekCommand(msg.RoomId, msg.Week), context.CancellationToken);
    }
}
