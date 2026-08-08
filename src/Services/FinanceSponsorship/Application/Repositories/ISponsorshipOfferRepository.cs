using ClubCraft.FinanceSponsorship.Domain.Aggregates;

namespace ClubCraft.FinanceSponsorship.Application.Repositories;

public interface ISponsorshipOfferRepository
{
    Task<SponsorshipOffer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SponsorshipOffer>> GetByClubIdAsync(Guid clubId, CancellationToken cancellationToken = default);
    Task AddAsync(SponsorshipOffer offer, CancellationToken cancellationToken = default);
    Task UpdateAsync(SponsorshipOffer offer, CancellationToken cancellationToken = default);
}
