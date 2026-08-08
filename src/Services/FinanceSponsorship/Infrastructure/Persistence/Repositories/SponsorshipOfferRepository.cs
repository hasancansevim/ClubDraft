using ClubCraft.FinanceSponsorship.Application.Repositories;
using ClubCraft.FinanceSponsorship.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.FinanceSponsorship.Infrastructure.Persistence.Repositories;

public class SponsorshipOfferRepository : ISponsorshipOfferRepository
{
    private readonly FinanceDbContext _context;

    public SponsorshipOfferRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SponsorshipOffer offer, CancellationToken cancellationToken = default)
    {
        await _context.SponsorshipOffers.AddAsync(offer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<SponsorshipOffer>> GetByClubIdAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        return await _context.SponsorshipOffers
            .Where(x => x.ClubId == clubId)
            .ToListAsync(cancellationToken);
    }

    public async Task<SponsorshipOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SponsorshipOffers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(SponsorshipOffer offer, CancellationToken cancellationToken = default)
    {
        _context.SponsorshipOffers.Update(offer);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
