using ClubCraft.ReputationFan.Domain.Aggregates;
using ClubCraft.ReputationFan.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.ReputationFan.Infrastructure.Persistence;

public class ReputationDbContext : DbContext
{
    public ReputationDbContext(DbContextOptions<ReputationDbContext> options) : base(options) { }

    public DbSet<ClubReputation> ClubReputations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Inbox/Outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // ClubReputation Config
        modelBuilder.Entity<ClubReputation>(b =>
        {
            b.HasKey(c => c.Id); // ClubId is PK

            b.Metadata.FindNavigation(nameof(ClubReputation.History))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            b.OwnsMany(c => c.History, h =>
            {
                h.WithOwner().HasForeignKey("ClubReputationId");
                h.Property(x => x.Id).ValueGeneratedOnAdd();
                h.HasKey(x => x.Id);
            });
        });
    }
}
