using ClubCraft.BuildingBlocks.Common.Enums;
using ClubCraft.MatchEngine.Domain.Enums;
using ClubCraft.MatchEngine.Domain.Services;

namespace ClubCraft.MatchEngine.Domain.Entities;

/// <summary>
/// MatchEngine'in ClubManagement'tan senkron sorgu atmadan (bkz. spec.md
/// §4.6) tuttugu, kulubun mac gucunu hesaplamaya yeten local read-model'i.
/// Roster (RosterPlayerSnapshot listesi) PlayerAddedToRosterEvent/
/// PlayerRemovedFromRosterEvent ile, dizilim (Formation + LineupSlots)
/// LineupUpdatedEvent ile senkron tutulur. Gercek TakimGucu hesabi burada
/// degil, IClubPowerCalculator'da yasar — bu sinif sadece veriyi tasir ve
/// kendi ic tutarliligini (Moral clamp'i gibi) korur.
/// </summary>
public class ClubPowerRating
{
    public Guid ClubId { get; private set; }
    public Guid RoomId { get; private set; }
    public string Formation { get; private set; } = FormationCatalog.DefaultFormation;

    /// <summary>
    /// Haftalik karardan (HireCoach/StadiumInvestment/MoraleBonus) gelen bonus
    /// — her mac sonrasi sifirlanir. Ama bu sifirlama SADECE kulubun o hafta
    /// gercekten bir mac oynamasina bagli (bkz. SimulateMatchesForWeekCommandHandler);
    /// bye haftasi gibi mac oynanmayan bir haftada karar alinirsa sifirlanmadan
    /// bir sonraki haftanin bonusunun ustune eklenir. <see cref="MoraleBonusCap"/>
    /// bu birikimin (kac hafta/karar arka arkaya gelirse gelsin) belirli bir
    /// tavani asmasini engelliyor — "denge duzeltmesi" notuna bkz. (spec.md,
    /// 2026-08-29).
    /// </summary>
    public int MoraleBonus { get; private set; }

    /// <summary>
    /// Tek bir haftada alinabilecek TUM haftalik kararlarin (HireCoach +
    /// StadiumInvestment + MoraleBonus, su anki eslemeyle 5+10+0=15) toplami
    /// zaten bu tavana esit — yani "bir haftada olabilecek en iyi durum"un
    /// USTUNE hicbir sekilde (ne art arda karar, ne mac atlanan bir hafta)
    /// cikilamiyor.
    /// </summary>
    private const int MoraleBonusCap = 15;

    /// <summary>
    /// Mac sonucu bazli, surekli guncellenen moral — [-5, +5] araliginda
    /// sinirli. MoraleBonus'tan (haftalik, tek seferlik) TAMAMEN ayri bir
    /// mekanizma; ikisi toplanip MoralModifiye'yi olusturur.
    /// </summary>
    public int Moral { get; private set; }

    private readonly List<RosterPlayerSnapshot> _roster = new();
    public IReadOnlyCollection<RosterPlayerSnapshot> Roster => _roster.AsReadOnly();

    private readonly List<LineupSlotAssignment> _lineupSlots = new();
    public IReadOnlyCollection<LineupSlotAssignment> LineupSlots => _lineupSlots.AsReadOnly();

    private const int MoralMin = -5;
    private const int MoralMax = 5;

    private ClubPowerRating() { } // EF Core

    public ClubPowerRating(Guid clubId, Guid roomId)
    {
        ClubId = clubId;
        RoomId = roomId;
    }

    public void AddPlayer(Guid playerId, int overall, PlayerPosition position)
    {
        if (_roster.Any(p => p.PlayerId == playerId))
            return; // idempotent: ayni oyuncu iki kez eklenmez (at-least-once teslimat)

        _roster.Add(new RosterPlayerSnapshot(playerId, overall, position));
    }

    public void RemovePlayer(Guid playerId)
    {
        _roster.RemoveAll(p => p.PlayerId == playerId);
    }

    public void UpdateLineup(string formation, IReadOnlyDictionary<string, Guid?> slots)
    {
        Formation = string.IsNullOrWhiteSpace(formation) ? FormationCatalog.DefaultFormation : formation;

        _lineupSlots.Clear();
        foreach (var (slotId, playerId) in slots)
        {
            _lineupSlots.Add(new LineupSlotAssignment(slotId, playerId));
        }
    }

    public void ApplyMoraleBonus(int bonus)
    {
        MoraleBonus = Math.Min(MoraleBonusCap, MoraleBonus + bonus);
    }

    public void ResetMoraleBonus()
    {
        MoraleBonus = 0;
    }

    /// <summary>
    /// Galibiyet -> +3 (max 5), Maglubiyet -> -3 (min -5), Beraberlik -> 0'a
    /// dogru 1 birim yaklasir. Her mac sonrasi (SimulateMatchesForWeekCommandHandler
    /// tarafindan) cagrilir.
    /// </summary>
    public void ApplyMatchResult(MatchOutcome outcome)
    {
        Moral = outcome switch
        {
            MatchOutcome.Win => Math.Min(MoralMax, Moral + 3),
            MatchOutcome.Loss => Math.Max(MoralMin, Moral - 3),
            MatchOutcome.Draw => Moral switch
            {
                > 0 => Moral - 1,
                < 0 => Moral + 1,
                _ => Moral
            },
            _ => Moral
        };
    }
}
