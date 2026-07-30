using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClubCraft.Draft.Infrastructure.Persistence;

public class DraftDbContextFactory : IDesignTimeDbContextFactory<DraftDbContext>
{
    public DraftDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DraftDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5434;Database=draft;Username=clubcraft;Password=clubcraft");

        return new DraftDbContext(optionsBuilder.Options);
    }
}
