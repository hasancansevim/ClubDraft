using ClubCraft.ReputationFan.Application.Repositories;
using ClubCraft.ReputationFan.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.ReputationFan.Infrastructure.Persistence.Repositories;

public class ClubReputationRepository : IClubReputationRepository
{
    private readonly ReputationDbContext _dbContext;

    public ClubReputationRepository(ReputationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClubReputation?> GetByIdAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClubReputations
            .Include(c => c.History)
            .FirstOrDefaultAsync(c => c.Id == clubId, cancellationToken);
    }

    public async Task SaveAsync(ClubReputation reputation, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(reputation);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.ClubReputations.Add(reputation);
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
