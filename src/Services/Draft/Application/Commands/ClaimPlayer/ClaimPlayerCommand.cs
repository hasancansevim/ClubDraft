using MediatR;

namespace ClubCraft.Draft.Application.Commands.ClaimPlayer;

public class ClaimPlayerCommand : IRequest<ClaimPlayerResult>
{
    public Guid DraftSessionId { get; set; }
    public Guid ClubId { get; set; }
    public Guid PlayerId { get; set; }
}
