using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClubCraft.Session.Infrastructure.Persistence
{
    public class SessionDbContextFactory : IDesignTimeDbContextFactory<SessionDbContext>
    {
        public SessionDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SessionDbContext>();
            
            // Connection string is not strictly important for generating migrations
            optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5433;Database=session;Username=clubcraft;Password=clubcraft");

            return new SessionDbContext(optionsBuilder.Options);
        }
    }
}
