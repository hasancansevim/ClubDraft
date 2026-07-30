using ClubCraft.Draft.Domain.Aggregates;
using ClubCraft.Draft.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.Draft.Infrastructure.Persistence;

public class DraftDbContext : DbContext
{
    public DraftDbContext(DbContextOptions<DraftDbContext> options) : base(options) { }

    public DbSet<DraftSession> DraftSessions { get; set; } = null!;
    public DbSet<DraftPick> DraftPicks { get; set; } = null!;
    public DbSet<DraftPlayerPoolItem> DraftPlayerPool { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Outbox configuration
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<DraftSession>(builder =>
        {
            builder.HasKey(x => x.Id);
            


            builder.HasMany(x => x.Picks)
                .WithOne()
                .HasForeignKey("DraftSessionId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(DraftSession.Picks))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.PlayerPool)
                .WithOne()
                .HasForeignKey("DraftSessionId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(DraftSession.PlayerPool))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            // Explicitly map the private primitive collection to avoid runtime issues with computed property
            builder.Property<List<Guid>>("_turnOrder")
                .HasColumnName("TurnOrder");
        });
        
        modelBuilder.Entity<DraftPlayerPoolItem>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.OwnsOne(x => x.Snapshot, s =>
            {
                s.Property(p => p.Name).HasColumnName("PlayerName");
                s.Property(p => p.Position).HasColumnName("PlayerPosition");
                s.Property(p => p.Overall).HasColumnName("PlayerOverall");
                s.Property(p => p.Age).HasColumnName("PlayerAge");
                s.Property(p => p.MarketValue).HasColumnName("PlayerMarketValue");
            });
        });
    }
}
