using ClubCraft.ReputationFan.Application.Repositories;
using ClubCraft.ReputationFan.Infrastructure.Persistence;
using ClubCraft.ReputationFan.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClubCraft.ReputationFan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ReputationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ReputationFanDb")));

        services.AddScoped<IClubReputationRepository, ClubReputationRepository>();

        return services;
    }
}
