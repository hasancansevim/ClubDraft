using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MassTransit;
using ClubCraft.BuildingBlocks.Sagas;

namespace ClubCraft.SagaOrchestrator.Persistence;

public class DraftPickStateMap : SagaClassMap<DraftPickState>
{
    protected override void Configure(EntityTypeBuilder<DraftPickState> entity, ModelBuilder model)
    {
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.DraftSessionId);
        entity.Property(x => x.ClubId);
        entity.Property(x => x.PlayerId);
        
        // If using optimistic concurrency, which we are with Version:
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
