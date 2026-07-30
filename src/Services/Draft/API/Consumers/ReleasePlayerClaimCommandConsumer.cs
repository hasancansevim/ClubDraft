using MassTransit;
using MediatR;
using ClubCraft.BuildingBlocks.Contracts.Commands;
using ClubCraft.Draft.Application.Commands.ReleasePlayerClaim;

namespace ClubCraft.Draft.API.Consumers;

public class ReleasePlayerClaimCommandConsumer : IConsumer<IReleasePlayerClaimCommand>
{
    private readonly IMediator _mediator;

    public ReleasePlayerClaimCommandConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IReleasePlayerClaimCommand> context)
    {
        var command = new ReleasePlayerClaimCommand(
            context.Message.PickAttemptId,
            context.Message.DraftSessionId,
            context.Message.PlayerId
        );

        await _mediator.Send(command);
    }
}
