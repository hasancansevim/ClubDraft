using Microsoft.AspNetCore.Mvc;
using MediatR;
using ClubCraft.ClubManagement.Application.Commands.InitializeClub;
using ClubCraft.ClubManagement.Application.Commands.MakeWeeklyDecision;
using ClubCraft.ClubManagement.Application.Queries.GetClub;

namespace ClubCraft.ClubManagement.API.Controllers;

[ApiController]
[Route("api/clubs")]
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

    [HttpGet("{clubId}")]
    public async Task<IActionResult> GetClub(Guid clubId)
    {
        var result = await _mediator.Send(new GetClubQuery { ClubId = clubId });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("{clubId}/weekly-decisions")]
    public async Task<IActionResult> MakeWeeklyDecision(Guid clubId, [FromBody] MakeWeeklyDecisionCommand command)
    {
        if (command.ClubId != clubId) return BadRequest("ClubId mismatch");
        var result = await _mediator.Send(command);
        if (result.Success)
        {
            return Ok(new { result.Cost });
        }
        return BadRequest(result.Reason);
    }

    [HttpPut("{clubId}/lineup")]
    public async Task<IActionResult> UpdateLineup(Guid clubId, [FromBody] ClubCraft.ClubManagement.Application.Commands.UpdateLineup.UpdateLineupCommand command)
    {
        if (command.ClubId != clubId) return BadRequest("ClubId mismatch");
        var success = await _mediator.Send(command);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPut("{clubId}/formation")]
    public async Task<IActionResult> UpdateFormation(Guid clubId, [FromBody] ClubCraft.ClubManagement.Application.Commands.UpdateFormation.UpdateFormationCommand command)
    {
        if (command.ClubId != clubId) return BadRequest("ClubId mismatch");
        var success = await _mediator.Send(command);
        if (!success) return NotFound();
        return Ok();
    }
}
