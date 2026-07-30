using Microsoft.AspNetCore.Mvc;
using MediatR;
using ClubCraft.ClubManagement.Application.Commands.InitializeClub;
using ClubCraft.ClubManagement.Application.Commands.MakeWeeklyDecision;

namespace ClubCraft.ClubManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClubController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClubController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeClub([FromBody] InitializeClubCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }

    [HttpPost("decision")]
    public async Task<IActionResult> MakeWeeklyDecision([FromBody] MakeWeeklyDecisionCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.Success)
        {
            return Ok(new { result.Cost });
        }
        return BadRequest(result.Reason);
    }
}
