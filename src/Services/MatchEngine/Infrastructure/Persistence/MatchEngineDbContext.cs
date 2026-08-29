using ClubCraft.MatchEngine.Domain.Aggregates;
using ClubCraft.MatchEngine.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.MatchEngine.Infrastructure.Persistence;

public class MatchEngineDbContext : DbContext
{
    public MatchEngineDbContext(DbContextOptions<MatchEngineDbContext> options) : base(options) { }

    public DbSet<Fixture> Fixtures { get; set; } = null!;
    public DbSet<ClubPowerRating> ClubPowerRatings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Inbox/Outbox
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Fixture Config
        modelBuilder.Entity<Fixture>(b =>
        {
            b.HasKey(f => f.Id);
            b.Property(f => f.RoomId).IsRequired();

            b.OwnsMany(f => f.Matches, m =>
            {
                m.WithOwner().HasForeignKey("FixtureId");
                m.HasKey("Id");
                m.Property(m => m.HomeClubId).IsRequired();
                m.Property(m => m.AwayClubId).IsRequired();
                m.Property(m => m.Week).IsRequired();
                
                m.OwnsMany(ma => ma.KeyEvents, k =>
                {
                    k.WithOwner().HasForeignKey("MatchId");
                    k.Property<int>("Id");
                    k.HasKey("Id");
                    k.Property(ke => ke.Type).HasConversion<int>();
                });
            });
        });

        // ClubPowerRating Config
        modelBuilder.Entity<ClubPowerRating>(b =>
        {
            b.HasKey(c => c.ClubId); // ClubId is PK
            b.Property(c => c.RoomId).IsRequired();
            b.Property(c => c.Formation).IsRequired();
            b.Property(c => c.MoraleBonus).IsRequired();
            b.Property(c => c.Moral).IsRequired();

            b.OwnsMany(c => c.Roster, r =>
            {
                r.WithOwner().HasForeignKey("ClubPowerRatingClubId");
                r.Property<int>("Id");
                r.HasKey("Id");
                r.Property(p => p.PlayerId).IsRequired();
                r.Property(p => p.Overall).IsRequired();
                r.Property(p => p.Position).HasConversion<string>().IsRequired();
            });

            b.OwnsMany(c => c.LineupSlots, s =>
            {
                s.WithOwner().HasForeignKey("ClubPowerRatingClubId");
                s.Property<int>("Id");
                s.HasKey("Id");
                s.Property(sl => sl.SlotId).IsRequired();
                s.Property(sl => sl.PlayerId);
            });
        });
    }
}
