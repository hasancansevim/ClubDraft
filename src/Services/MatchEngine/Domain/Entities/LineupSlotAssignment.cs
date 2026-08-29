namespace ClubCraft.MatchEngine.Domain.Entities;

/// <summary>
/// ClubPowerRating'in kendi lineup kopyasindaki tek bir formasyon slotu.
/// LineupUpdatedEvent'ten gelir (SlotId, orn. "CB1" -> o slota atanmis
/// oyuncunun PlayerId'si; slot bossa PlayerId null). Slot ID -> gerekli
/// pozisyon eslemesi burada degil, FormationCatalog'da yasar.
/// </summary>
public class LineupSlotAssignment
{
    public string SlotId { get; private set; } = string.Empty;
    public Guid? PlayerId { get; private set; }

    private LineupSlotAssignment() { } // EF Core

    public LineupSlotAssignment(string slotId, Guid? playerId)
    {
        SlotId = slotId;
        PlayerId = playerId;
    }
}
