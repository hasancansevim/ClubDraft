using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.Draft.Domain.ValueObjects;

namespace ClubCraft.Draft.Domain.Entities;

public class DraftPlayerPoolItem : Entity<Guid>
{
    public Guid PlayerId { get; private set; }
    public PlayerSnapshot Snapshot { get; private set; }
    public bool IsClaimed { get; private set; }

    private DraftPlayerPoolItem() { } // EF Core

    public DraftPlayerPoolItem(Guid playerId, PlayerSnapshot snapshot)
    {
        Id = Guid.NewGuid();
        PlayerId = playerId;
        Snapshot = snapshot;
        IsClaimed = false;
    }

    public void MarkAsClaimed()
    {
        if (IsClaimed)
            throw new InvalidOperationException("Player is already claimed.");

        IsClaimed = true;
    }

    public void RevertClaim()
    {
        if (!IsClaimed)
            throw new InvalidOperationException("Player is not claimed.");

        IsClaimed = false;
    }
}
