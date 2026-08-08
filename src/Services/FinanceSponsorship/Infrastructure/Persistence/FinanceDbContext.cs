using ClubCraft.FinanceSponsorship.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.FinanceSponsorship.Infrastructure.Persistence;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<SponsorshipOffer> SponsorshipOffers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();

        builder.Entity<SponsorshipOffer>().HasKey(o => o.Id);
        
        builder.Entity<SponsorshipOffer>()
            .HasIndex(o => new { o.ClubId, o.ThresholdReached })
            .IsUnique();
    }
}
