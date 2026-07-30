using MediatR;

namespace ClubCraft.Draft.Application.Commands.ReleasePlayerClaim;

public record ReleasePlayerClaimCommand(Guid PickAttemptId, Guid DraftSessionId, Guid PlayerId) : IRequest;
