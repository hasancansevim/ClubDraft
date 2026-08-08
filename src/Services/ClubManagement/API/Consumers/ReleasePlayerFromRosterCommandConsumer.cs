using MassTransit;
using MediatR;
using ClubCraft.BuildingBlocks.Contracts.Commands;
using ClubCraft.ClubManagement.Application.Commands.ReleasePlayerFromRoster;

namespace ClubCraft.ClubManagement.API.Consumers;

public class ReleasePlayerFromRosterCommandConsumer : IConsumer<IReleasePlayerFromRosterCommand>
{
    private readonly IMediator _mediator;

    public ReleasePlayerFromRosterCommandConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IReleasePlayerFromRosterCommand> context)
    {
        var command = new ReleasePlayerFromRosterCommand(
            context.Message.ClubId,
            context.Message.PlayerId,
            context.Message.PickAttemptId
        );

        await _mediator.Send(command);
    }
}
