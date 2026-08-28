using MediatR;

namespace ClubCraft.ClubManagement.Application.Commands.UpdateFormation;

public class UpdateFormationCommand : IRequest<bool>
{
    public Guid ClubId { get; set; }
    public string Formation { get; set; } = "4-4-2";
}
