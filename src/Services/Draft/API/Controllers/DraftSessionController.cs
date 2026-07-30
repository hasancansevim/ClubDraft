using ClubCraft.Draft.Application.Commands.ClaimPlayer;
using ClubCraft.Draft.Application.Commands.StartDraft;
using ClubCraft.Draft.Application.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClubCraft.Draft.API.Controllers;

[ApiController]
[Route("draft-sessions")]
public class DraftSessionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IDraftSessionRepository _repository;

    public DraftSessionController(IMediator mediator, IDraftSessionRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    [HttpPost("{roomId}/start")]
    public async Task<IActionResult> StartDraft(Guid roomId, [FromBody] StartDraftRequest request)
    {
        // Notice: In the real app, roomId might map to DraftSessionId 1-to-1 or we query it.
        // Assuming DraftSessionId == RoomId for simplicity here, or you could pass DraftSessionId.
        var command = new StartDraftCommand
        {
            DraftSessionId = roomId,
            TurnOrder = request.TurnOrder
        };

        await _mediator.Send(command);
        return Ok();
    }

    [HttpGet("{draftSessionId}/pool")]
    public async Task<IActionResult> GetPlayerPool(Guid draftSessionId)
    {
        var session = await _repository.GetByIdAsync(draftSessionId);
        if (session == null) return NotFound();

        var pool = session.PlayerPool.Select(p => new
        {
            p.PlayerId,
            p.Snapshot.Name,
            p.Snapshot.Position,
            p.Snapshot.Overall,
            p.Snapshot.Age,
            p.Snapshot.MarketValue,
            p.IsClaimed
        });

        return Ok(pool);
    }

    [HttpPost("{draftSessionId}/claim")]
    public async Task<IActionResult> ClaimPlayer(Guid draftSessionId, [FromBody] ClaimPlayerRequest request)
    {
        var command = new ClaimPlayerCommand
        {
            DraftSessionId = draftSessionId,
            ClubId = request.ClubId,
            PlayerId = request.PlayerId
        };

        var result = await _mediator.Send(command);

        return Ok(new
        {
            success = result.Success,
            pickNumber = result.PickNumber,
            reason = result.Reason
        });
    }

    [HttpGet("{draftSessionId}/state")]
    public async Task<IActionResult> GetState(Guid draftSessionId)
    {
        var session = await _repository.GetByIdAsync(draftSessionId);
        if (session == null) return NotFound();

        return Ok(new
        {
            session.Id,
            session.RoomId,
            Status = session.Status.ToString(),
            session.CurrentPickIndex,
            TurnOrder = session.TurnOrder,
            Picks = session.Picks.Select(p => new
            {
                p.PickNumber,
                p.ClubId,
                p.PlayerId,
                p.ClaimedAt
            })
        });
    }
}

public class StartDraftRequest
{
    public List<Guid> TurnOrder { get; set; } = new();
}

public class ClaimPlayerRequest
{
    public Guid ClubId { get; set; }
    public Guid PlayerId { get; set; }
}
