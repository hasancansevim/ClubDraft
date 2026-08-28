using MediatR;

namespace ClubCraft.ClubManagement.Application.Commands.UpdateLineup;

public class UpdateLineupCommand : IRequest<bool>
{
    public Guid ClubId { get; set; }
    public string LineupJson { get; set; } = "{}";
}
