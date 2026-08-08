using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Commands.GenerateFixture;
using MassTransit;
using MediatR;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class DraftCompletedEventConsumer : IConsumer<IDraftCompletedEvent>
{
    private readonly IMediator _mediator;

    public DraftCompletedEventConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IDraftCompletedEvent> context)
    {
        var msg = context.Message;
        
        // Use Idempotency (Inbox pattern is handled by MassTransit EF Core integration configured in Program.cs)
        await _mediator.Send(new GenerateFixtureCommand(msg.DraftSessionId, msg.ClubIds.ToList()), context.CancellationToken);
    }
}
