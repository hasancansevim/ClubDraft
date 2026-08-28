using MassTransit;
using MediatR;
using ClubCraft.BuildingBlocks.Contracts.Commands;
using ClubCraft.ClubManagement.Application.Commands.AddPlayerToRoster;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ClubCraft.ClubManagement.API.Consumers;

public class AddPlayerToRosterCommandConsumer : IConsumer<IAddPlayerToRosterCommand>
{
    private readonly IMediator _mediator;
    private readonly ILogger<AddPlayerToRosterCommandConsumer> _logger;

    public AddPlayerToRosterCommandConsumer(IMediator mediator, ILogger<AddPlayerToRosterCommandConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IAddPlayerToRosterCommand> context)
    {
        var sw = Stopwatch.StartNew();

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

        sw.Stop();
        var level = sw.ElapsedMilliseconds > 5000
            ? LogLevel.Warning   // 5 saniyeden uzunsa uyar — saga timeout riski
            : LogLevel.Information;

        _logger.Log(level,
            "[PERF] AddPlayerToRoster completed in {ElapsedMs}ms | PickAttemptId={PickAttemptId} ClubId={ClubId} PlayerId={PlayerId}",
            sw.ElapsedMilliseconds,
            context.Message.PickAttemptId,
            context.Message.ClubId,
            context.Message.PlayerId);
    }
}
