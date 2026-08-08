using ClubCraft.FinanceSponsorship.Application.Repositories;
using ClubCraft.FinanceSponsorship.Infrastructure.Persistence;
using ClubCraft.FinanceSponsorship.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClubCraft.FinanceSponsorship.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FinanceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("FinanceSponsorshipDb")));

        services.AddScoped<ISponsorshipOfferRepository, SponsorshipOfferRepository>();

        return services;
    }
}
