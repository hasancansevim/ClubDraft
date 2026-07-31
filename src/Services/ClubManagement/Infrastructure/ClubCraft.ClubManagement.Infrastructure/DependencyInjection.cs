using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.ClubManagement.Infrastructure.Persistence;

namespace ClubCraft.ClubManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ClubDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IClubRepository, ClubRepository>();

        return services;
    }
}
