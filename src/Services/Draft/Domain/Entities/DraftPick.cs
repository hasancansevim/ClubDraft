using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.Draft.Domain.Entities;

public class DraftPick : Entity<Guid>
{
    public int PickNumber { get; private set; }
    public Guid ClubId { get; private set; }
    public Guid PlayerId { get; private set; }
    public DateTime ClaimedAt { get; private set; }

    private DraftPick() { } // EF Core

    public DraftPick(int pickNumber, Guid clubId, Guid playerId, DateTime claimedAt)
    {
        Id = Guid.NewGuid();
        PickNumber = pickNumber;
        ClubId = clubId;
        PlayerId = playerId;
        ClaimedAt = claimedAt;
    }
}
