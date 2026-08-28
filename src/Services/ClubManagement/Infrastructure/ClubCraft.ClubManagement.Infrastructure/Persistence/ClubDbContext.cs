using Microsoft.EntityFrameworkCore;
using MassTransit;
using ClubCraft.ClubManagement.Domain.Aggregates;
using ClubCraft.ClubManagement.Domain.Entities;
using ClubCraft.ClubManagement.Domain.ValueObjects;

namespace ClubCraft.ClubManagement.Infrastructure.Persistence;

public class ClubDbContext : DbContext
{
    public ClubDbContext(DbContextOptions<ClubDbContext> options) : base(options)
    {
    }

    public DbSet<Club> Clubs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Club>(b =>
        {
            b.HasKey(c => c.Id);
            
            b.OwnsOne(c => c.Budget, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Budget_Amount");
            });

            // OwnsMany for Player Roster
            b.OwnsMany(c => c.Roster, p =>
            {
                p.ToTable("ClubRoster");
                p.WithOwner().HasForeignKey("ClubId");
                p.HasKey("Id");
                p.Property("Id").ValueGeneratedNever();
                p.Property("Name").IsRequired();
                p.Property<ClubCraft.BuildingBlocks.Common.Enums.PlayerPosition>("Position").IsRequired().HasConversion<string>();
                p.Property("Overall");
                p.Property("Age");
                p.Property("MarketValue");
            });

            // OwnsMany for WeeklyDecisions
            b.OwnsMany(c => c.WeeklyDecisions, d =>
            {
                d.ToTable("ClubWeeklyDecisions");
                d.WithOwner().HasForeignKey("ClubId");
                d.HasKey("Id");
                d.Property("Id").ValueGeneratedNever();
                d.Property("Week");
                d.Property("Type");
                d.OwnsOne(typeof(Money), "Cost", cost =>
                {
                    cost.Property("Amount").HasColumnName("Cost");
                });
                d.Property("DecidedAt");
            });

            b.Navigation(c => c.Roster).Metadata.SetField("_roster");
            b.Navigation(c => c.Roster).UsePropertyAccessMode(PropertyAccessMode.Field);

            b.Navigation(c => c.WeeklyDecisions).Metadata.SetField("_weeklyDecisions");
            b.Navigation(c => c.WeeklyDecisions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
