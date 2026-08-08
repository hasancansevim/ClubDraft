using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClubCraft.MatchEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MatchEngineDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("MatchEngineDb")));

        services.AddScoped<IFixtureRepository, FixtureRepository>();
        services.AddScoped<IClubPowerRatingRepository, ClubPowerRatingRepository>();

        return services;
    }
}
