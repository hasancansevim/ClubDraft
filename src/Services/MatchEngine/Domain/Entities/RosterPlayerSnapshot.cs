using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.MatchEngine.Domain.Entities;

/// <summary>
/// ClubPowerRating'in kendi roster kopyasindaki tek bir oyuncu satiri.
/// PlayerAddedToRosterEvent ile eklenir, PlayerRemovedFromRosterEvent ile
/// (PlayerId'ye gore) silinir. Overall + Position, IClubPowerCalculator'in
/// Ilk 11 + pozisyon uyumu hesaplayabilmesi icin gereken minimum veridir —
/// ClubManagement'a senkron sorgu atilmadan (bkz. spec.md §4.6).
/// </summary>
public class RosterPlayerSnapshot
{
    public Guid PlayerId { get; private set; }
    public int Overall { get; private set; }
    public PlayerPosition Position { get; private set; }

    private RosterPlayerSnapshot() { } // EF Core

    public RosterPlayerSnapshot(Guid playerId, int overall, PlayerPosition position)
    {
        PlayerId = playerId;
        Overall = overall;
        Position = position;
    }
}
