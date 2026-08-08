using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.MatchEngine.Infrastructure.Persistence;

public class ClubPowerRatingRepository : IClubPowerRatingRepository
{
    private readonly MatchEngineDbContext _dbContext;

    public ClubPowerRatingRepository(MatchEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ClubPowerRating?> GetByIdAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClubPowerRatings
            .FirstOrDefaultAsync(c => c.ClubId == clubId, cancellationToken);
    }

    public async Task SaveAsync(ClubPowerRating clubPowerRating, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(clubPowerRating).State == EntityState.Detached)
        {
            _dbContext.ClubPowerRatings.Add(clubPowerRating);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
