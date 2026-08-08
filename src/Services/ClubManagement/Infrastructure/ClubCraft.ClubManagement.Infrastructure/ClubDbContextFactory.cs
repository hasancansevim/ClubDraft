using ClubCraft.ClubManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClubCraft.ClubManagement.Infrastructure;

public class ClubDbContextFactory : IDesignTimeDbContextFactory<ClubDbContext>
{
    public ClubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubDbContext>();
        optionsBuilder.UseNpgsql("Host=127.0.0.1;Port=5435;Database=clubmanagement;Username=clubcraft;Password=clubcraft;");

        return new ClubDbContext(optionsBuilder.Options);
    }
}
