using ClubCraft.ClubManagement.Domain.Aggregates;

namespace ClubCraft.ClubManagement.Application.Repositories;

public interface IClubRepository
{
    Task<Club?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Club?> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task SaveAsync(Club club, CancellationToken cancellationToken = default);
}
