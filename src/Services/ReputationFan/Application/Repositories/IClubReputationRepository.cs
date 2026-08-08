using ClubCraft.ReputationFan.Domain.Aggregates;

namespace ClubCraft.ReputationFan.Application.Repositories;

public interface IClubReputationRepository
{
    Task<ClubReputation?> GetByIdAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task SaveAsync(ClubReputation reputation, CancellationToken cancellationToken = default);
}
