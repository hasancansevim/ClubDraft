using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.ClubManagement.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.ClubManagement.Infrastructure.Persistence;

public class ClubRepository : IClubRepository
{
    private readonly ClubDbContext _dbContext;

    public ClubRepository(ClubDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Club?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clubs
            .Include(c => c.Roster)
            .Include(c => c.WeeklyDecisions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task SaveAsync(Club club, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(club);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Clubs.Add(club);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

}
