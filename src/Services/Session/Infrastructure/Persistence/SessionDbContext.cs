using ClubCraft.Session.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace ClubCraft.Session.Infrastructure.Persistence
{
    public class SessionDbContext : DbContext
    {
        public SessionDbContext(DbContextOptions<SessionDbContext> options) : base(options)
        {
        }

        public DbSet<GameRoom> GameRooms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GameRoom>(builder =>
            {
                builder.HasKey(g => g.Id);
                builder.Property("Version").IsRowVersion();
                builder.Ignore(g => g.RowVersion);
                builder.Ignore(g => g.DomainEvents);

                builder.HasIndex(g => g.ShortCode).IsUnique();

                builder.HasMany(g => g.Participants)
                    .WithOne()
                    .HasForeignKey("GameRoomId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Participant>(builder =>
            {
                builder.HasKey(p => p.Id);
                builder.Property(p => p.Id).ValueGeneratedNever();
            });

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
        }
    }
}
