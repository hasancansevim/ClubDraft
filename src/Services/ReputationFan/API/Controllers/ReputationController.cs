using Microsoft.AspNetCore.Mvc;
using MediatR;
using ClubCraft.ReputationFan.Application.Queries.GetReputation;

namespace ClubCraft.ReputationFan.API.Controllers;

[ApiController]
[Route("api/reputation")]
public class ReputationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReputationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{clubId}")]
    public async Task<IActionResult> GetReputation(Guid clubId)
    {
        var result = await _mediator.Send(new GetReputationQuery { ClubId = clubId });
        return Ok(new { score = result });
    }
}
