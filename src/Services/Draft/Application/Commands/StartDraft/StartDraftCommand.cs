using MediatR;

namespace ClubCraft.Draft.Application.Commands.StartDraft;

public class StartDraftCommand : IRequest<bool>
{
    public Guid DraftSessionId { get; set; }
    public List<Guid> TurnOrder { get; set; } = new();
}
