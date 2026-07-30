using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using ClubCraft.BuildingBlocks.Sagas;

namespace ClubCraft.SagaOrchestrator.Persistence;

public class DraftPickStateDbContext : SagaDbContext
{
    public DraftPickStateDbContext(DbContextOptions<DraftPickStateDbContext> options) : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new DraftPickStateMap(); }
    }
}
