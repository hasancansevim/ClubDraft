using ClubCraft.BuildingBlocks.Common.Enums;
using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

/// <summary>
/// Match Engine guc formulu (spec.md, "Match Engine Guc Formulu
/// Derinlestirme"):
///
///   BazGuc = Sum(Oyuncu Overall x PozisyonUyumCarpani) / 11   (Ilk 11 uzerinden)
///   YildizBonusu = 0.15 x (EnYuksekOverall - BazGuc)           (Ilk 11'deki en yuksek ham Overall)
///   DerinlikBonusu = 0.05 x (Yedeklerin ortalama Overall'i)
///   TakimGucu = BazGuc + YildizBonusu + DerinlikBonusu + MoralModifiye
///   MoralModifiye = MoraleBonus (haftalik, tek seferlik) + Moral (mac sonucu bazli, surekli)
/// </summary>
public class ClubPowerCalculator : IClubPowerCalculator
{
    public ClubPowerBreakdown Calculate(ClubPowerRating rating)
    {
        if (rating.Roster.Count < 11)
        {
            throw new InvalidOperationException(
                $"Club {rating.ClubId} icin Ilk 11 kurulamaz: roster'da sadece {rating.Roster.Count} oyuncu var (en az 11 gerekli).");
        }

        var starters = ResolveStartingEleven(rating);

        var weightedSum = starters.Sum(s => s.Player.Overall * PositionCompatibility.Multiplier(s.Player.Position, s.RequiredPosition));
        var bazGuc = weightedSum / starters.Count;

        var enYuksekOverall = starters.Max(s => s.Player.Overall);
        var yildizBonusu = 0.15 * (enYuksekOverall - bazGuc);

        var starterIds = starters.Select(s => s.Player.PlayerId).ToHashSet();
        var bench = rating.Roster.Where(p => !starterIds.Contains(p.PlayerId)).ToList();
        var derinlikBonusu = bench.Count > 0 ? 0.05 * bench.Average(p => p.Overall) : 0;

        var moralModifiye = rating.MoraleBonus + rating.Moral;

        var teamPower = bazGuc + yildizBonusu + derinlikBonusu + moralModifiye;

        return new ClubPowerBreakdown(bazGuc, yildizBonusu, derinlikBonusu, moralModifiye, teamPower);
    }

    /// <summary>
    /// Formasyonun her slotu icin: lineup'ta o slota gercekten atanmis
    /// (ve roster'da hala mevcut, ve baska bir slota da atanmamis) bir
    /// oyuncu varsa onu kullanir. Yoksa (lineup hic kurulmamis / slot bos /
    /// atanan oyuncu roster'dan cikmis) o slotu roster'dan kalan en iyi
    /// oyuncuyla otomatik doldurur — once pozisyonu tam uyanla, yoksa en
    /// yuksek Overall'liyla. Bu, "kullanici lineup'i hic ayarlamadi" durumunu
    /// bir dizilim-hatasi gibi cezalandirmiyor; bilerek kurulmus bozuk bir
    /// dizilim (orn. GK'yi ST slotuna koymak) yine dusuk carpan aliyor
    /// cunku o slot icin lineup'ta ACIKCA bir atama var.
    /// </summary>
    private static List<(string RequiredSlotId, PlayerPosition RequiredPosition, RosterPlayerSnapshot Player)> ResolveStartingEleven(ClubPowerRating rating)
    {
        var slots = FormationCatalog.Resolve(rating.Formation);
        var lineupBySlot = rating.LineupSlots.ToDictionary(s => s.SlotId, s => s.PlayerId);
        var rosterById = rating.Roster.ToDictionary(p => p.PlayerId);

        var used = new HashSet<Guid>();
        var starters = new List<(string, PlayerPosition, RosterPlayerSnapshot)>();
        var unresolvedSlots = new List<(string SlotId, PlayerPosition RequiredPosition)>();

        foreach (var (slotId, requiredPosition) in slots)
        {
            if (lineupBySlot.TryGetValue(slotId, out var playerId)
                && playerId.HasValue
                && rosterById.TryGetValue(playerId.Value, out var player)
                && used.Add(playerId.Value))
            {
                starters.Add((slotId, requiredPosition, player));
            }
            else
            {
                unresolvedSlots.Add((slotId, requiredPosition));
            }
        }

        if (unresolvedSlots.Count > 0)
        {
            var remaining = rating.Roster.Where(p => !used.Contains(p.PlayerId)).ToList();

            foreach (var (slotId, requiredPosition) in unresolvedSlots)
            {
                var best = remaining.Where(p => p.Position == requiredPosition).OrderByDescending(p => p.Overall).FirstOrDefault()
                    ?? remaining.OrderByDescending(p => p.Overall).FirstOrDefault();

                if (best == null)
                    break; // roster tukendi (11 guard'i nedeniyle normalde olmaz)

                starters.Add((slotId, requiredPosition, best));
                used.Add(best.PlayerId);
                remaining.Remove(best);
            }
        }

        return starters;
    }
}
