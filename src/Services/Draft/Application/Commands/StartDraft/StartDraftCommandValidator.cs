using FluentValidation;

namespace ClubCraft.Draft.Application.Commands.StartDraft;

public class StartDraftCommandValidator : AbstractValidator<StartDraftCommand>
{
    public StartDraftCommandValidator()
    {
        RuleFor(x => x.DraftSessionId).NotEmpty().WithMessage("Draft Session ID cannot be empty.");
        RuleFor(x => x.TurnOrder).NotEmpty().WithMessage("Turn order must be provided.");
        RuleFor(x => x.TurnOrder).Must(x => x.Count > 0).WithMessage("Turn order cannot be empty.");
    }
}
