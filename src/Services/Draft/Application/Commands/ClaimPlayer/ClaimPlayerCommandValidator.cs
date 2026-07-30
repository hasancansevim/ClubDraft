using FluentValidation;

namespace ClubCraft.Draft.Application.Commands.ClaimPlayer;

public class ClaimPlayerCommandValidator : AbstractValidator<ClaimPlayerCommand>
{
    public ClaimPlayerCommandValidator()
    {
        RuleFor(x => x.DraftSessionId).NotEmpty().WithMessage("Draft Session ID cannot be empty.");
        RuleFor(x => x.ClubId).NotEmpty().WithMessage("Club ID cannot be empty.");
        RuleFor(x => x.PlayerId).NotEmpty().WithMessage("Player ID cannot be empty.");
    }
}
