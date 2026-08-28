using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Application.Repositories;

public interface IClubPowerRatingRepository
{
    Task<ClubPowerRating?> GetByIdAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task<List<ClubPowerRating>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task SaveAsync(ClubPowerRating clubPowerRating, CancellationToken cancellationToken = default);
}
