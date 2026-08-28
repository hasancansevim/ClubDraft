using Microsoft.AspNetCore.Mvc;
using MediatR;
using ClubCraft.MatchEngine.Application.Queries.GetStandings;

namespace ClubCraft.MatchEngine.API.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MatchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{roomId}/standings")]
    public async Task<IActionResult> GetStandings(Guid roomId)
    {
        var result = await _mediator.Send(new GetStandingsQuery { RoomId = roomId });
        return Ok(result);
    }
}
