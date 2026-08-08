using ClubCraft.MatchEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClubCraft.MatchEngine.Infrastructure;

public class MatchEngineDbContextFactory : IDesignTimeDbContextFactory<MatchEngineDbContext>
{
    public MatchEngineDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        
        // Go up to API folder if running from Infrastructure
        if (basePath.EndsWith("ClubCraft.MatchEngine.Infrastructure"))
        {
            basePath = Path.Combine(basePath, "..", "API", "ClubCraft.MatchEngine.API");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MatchEngineDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("MatchEngineDb"));

        return new MatchEngineDbContext(optionsBuilder.Options);
    }
}
