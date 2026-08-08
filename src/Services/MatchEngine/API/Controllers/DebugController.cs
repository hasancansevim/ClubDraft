using ClubCraft.MatchEngine.Application.Commands.GenerateFixture;
using ClubCraft.MatchEngine.Application.Commands.SimulateMatchesForWeek;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClubCraft.MatchEngine.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly IMediator _mediator;

    public DebugController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("generate-fixture")]
    public async Task<IActionResult> GenerateFixture([FromBody] GenerateFixtureRequest request)
    {
        await _mediator.Send(new GenerateFixtureCommand(request.RoomId, request.ClubIds));
        return Ok();
    }

    [HttpPost("simulate-week")]
    public async Task<IActionResult> SimulateWeek([FromBody] SimulateWeekRequest request)
    {
        await _mediator.Send(new SimulateMatchesForWeekCommand(request.RoomId, request.Week));
        return Ok();
    }
}

public class GenerateFixtureRequest
{
    public Guid RoomId { get; set; }
    public List<Guid> ClubIds { get; set; } = new();
}

public class SimulateWeekRequest
{
    public Guid RoomId { get; set; }
    public int Week { get; set; }
}
