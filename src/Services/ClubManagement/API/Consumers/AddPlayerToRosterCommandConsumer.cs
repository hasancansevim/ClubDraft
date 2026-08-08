using MassTransit;
using MediatR;
using ClubCraft.BuildingBlocks.Contracts.Commands;
using ClubCraft.ClubManagement.Application.Commands.AddPlayerToRoster;

namespace ClubCraft.ClubManagement.API.Consumers;

public class AddPlayerToRosterCommandConsumer : IConsumer<IAddPlayerToRosterCommand>
{
    private readonly IMediator _mediator;

    public AddPlayerToRosterCommandConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IAddPlayerToRosterCommand> context)
    {
        var command = new AddPlayerToRosterCommand(
            context.Message.ClubId,
            context.Message.PlayerId,
            context.Message.Name,
            context.Message.Position,
            context.Message.Overall,
            context.Message.Age,
            context.Message.MarketValue,
            context.Message.PickAttemptId
        );

        await _mediator.Send(command);
    }
}
