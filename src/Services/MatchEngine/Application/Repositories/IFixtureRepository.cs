using ClubCraft.MatchEngine.Domain.Aggregates;

namespace ClubCraft.MatchEngine.Application.Repositories;

public interface IFixtureRepository
{
    Task<Fixture?> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task SaveAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
