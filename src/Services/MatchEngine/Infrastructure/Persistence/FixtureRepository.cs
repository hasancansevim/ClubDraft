using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.MatchEngine.Infrastructure.Persistence;

public class FixtureRepository : IFixtureRepository
{
    private readonly MatchEngineDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public FixtureRepository(MatchEngineDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Fixture?> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Fixtures
            .Include(f => f.Matches)
            .ThenInclude(m => m.KeyEvents)
            .FirstOrDefaultAsync(f => f.RoomId == roomId, cancellationToken);
    }

    public async Task SaveAsync(Fixture fixture, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(fixture).State == EntityState.Detached)
        {
            _dbContext.Fixtures.Add(fixture);
        }

        // Publish domain events before SaveChangesAsync for Outbox pattern
        foreach (var domainEvent in fixture.DomainEvents)
        {
            await _publishEndpoint.Publish((object)domainEvent, cancellationToken);
        }
        fixture.ClearDomainEvents();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
