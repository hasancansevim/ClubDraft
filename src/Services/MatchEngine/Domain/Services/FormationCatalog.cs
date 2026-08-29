using ClubCraft.BuildingBlocks.Common.Enums;

namespace ClubCraft.MatchEngine.Domain.Services;

/// <summary>
/// Pozisyon ailesi (Pozisyon Uyum Carpani tablosunun satir/sutunlari).
/// GK kendi basina bir aile — hicbir aileye "komsu" sayilmaz (bkz.
/// PositionCompatibility.Multiplier).
/// </summary>
public enum PositionFamily
{
    Goalkeeper,
    Defense,
    Midfield,
    Attack
}

/// <summary>
/// Formasyon slot ID'si -> o slotun gerektirdigi pozisyon eslemesi.
///
/// ⚠️ frontend/src/constants/formations.ts'teki FORMATIONS sabitiyle
/// BIREBIR ayni slot ID'lerini tasimak zorunda — kullanici lineup'i
/// frontend'de bu ID'lerle kaydediyor (Club.LineupJson), MatchEngine bu
/// ID'leri LineupUpdatedEvent uzerinden aynen aliyor. Formasyon frontend'de
/// degistirilirse burasi da elle guncellenmeli (iki farkli dil/servis
/// oldugu icin paylasilan bir dosyaya tasinamiyor, bkz. spec.md'deki
/// PlayerPosition/formasyon tek-kaynak notlari).
/// </summary>
public static class FormationCatalog
{
    public const string DefaultFormation = "4-4-2";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string SlotId, PlayerPosition RequiredPosition)>> Formations =
        new Dictionary<string, IReadOnlyList<(string, PlayerPosition)>>
        {
            ["4-4-2"] = new List<(string, PlayerPosition)>
            {
                ("GK", PlayerPosition.GK),
                ("LB", PlayerPosition.LB),
                ("CB1", PlayerPosition.CB),
                ("CB2", PlayerPosition.CB),
                ("RB", PlayerPosition.RB),
                ("LM", PlayerPosition.LM),
                ("CM1", PlayerPosition.CM),
                ("CM2", PlayerPosition.CM),
                ("RM", PlayerPosition.RM),
                ("ST1", PlayerPosition.ST),
                ("ST2", PlayerPosition.ST),
            },
            ["4-3-3"] = new List<(string, PlayerPosition)>
            {
                ("GK", PlayerPosition.GK),
                ("LB", PlayerPosition.LB),
                ("CB1", PlayerPosition.CB),
                ("CB2", PlayerPosition.CB),
                ("RB", PlayerPosition.RB),
                ("CDM", PlayerPosition.CDM),
                ("CM1", PlayerPosition.CM),
                ("CM2", PlayerPosition.CM),
                ("LW", PlayerPosition.LW),
                ("ST", PlayerPosition.ST),
                ("RW", PlayerPosition.RW),
            },
            ["4-2-3-1"] = new List<(string, PlayerPosition)>
            {
                ("GK", PlayerPosition.GK),
                ("LB", PlayerPosition.LB),
                ("CB1", PlayerPosition.CB),
                ("CB2", PlayerPosition.CB),
                ("RB", PlayerPosition.RB),
                ("CDM1", PlayerPosition.CDM),
                ("CDM2", PlayerPosition.CDM),
                ("LW", PlayerPosition.LW),
                ("CAM", PlayerPosition.CAM),
                ("RW", PlayerPosition.RW),
                ("ST", PlayerPosition.ST),
            },
            ["3-5-2"] = new List<(string, PlayerPosition)>
            {
                ("GK", PlayerPosition.GK),
                ("CB1", PlayerPosition.CB),
                ("CB2", PlayerPosition.CB),
                ("CB3", PlayerPosition.CB),
                ("LWB", PlayerPosition.LWB),
                ("CDM", PlayerPosition.CDM),
                ("CM", PlayerPosition.CM),
                ("CAM", PlayerPosition.CAM),
                ("RWB", PlayerPosition.RWB),
                ("ST1", PlayerPosition.ST),
                ("ST2", PlayerPosition.ST),
            },
        };

    /// <summary>Bilinmeyen/bos bir formasyon kodu icin varsayilan 4-4-2'ye duser.</summary>
    public static IReadOnlyList<(string SlotId, PlayerPosition RequiredPosition)> Resolve(string? formation)
    {
        if (formation != null && Formations.TryGetValue(formation, out var slots))
            return slots;

        return Formations[DefaultFormation];
    }
}

/// <summary>
/// Pozisyon Uyum Carpani (spec.md, "Denge duzeltmesi: pozisyon carpani
/// sertlestirme ve haftalik karar tavani" — 2026-08-29): tam eslesme 1.00,
/// ayni aile farkli pozisyon 0.70, komsu aile (Defans↔OrtaSaha,
/// OrtaSaha↔Hucum) 0.35, uzak aile (Defans↔Hucum, GK↔herhangi biri) 0.10.
/// Eski tablo (1.00/0.85/0.65/0.40) tamamen yanlis dizilmis bir takimi
/// yeterince cezalandirmiyordu — haftalik karar bonusuyla birlikte rekabetci
/// kalabiliyordu (bkz. ayni tarihli tavan notu, ClubPowerRating.MoraleBonus).
/// </summary>
public static class PositionCompatibility
{
    public static PositionFamily FamilyOf(PlayerPosition position) => position switch
    {
        PlayerPosition.GK => PositionFamily.Goalkeeper,
        PlayerPosition.CB or PlayerPosition.RB or PlayerPosition.LB or PlayerPosition.RWB or PlayerPosition.LWB => PositionFamily.Defense,
        PlayerPosition.CDM or PlayerPosition.CM or PlayerPosition.CAM or PlayerPosition.RM or PlayerPosition.LM => PositionFamily.Midfield,
        PlayerPosition.RW or PlayerPosition.LW or PlayerPosition.ST or PlayerPosition.CF => PositionFamily.Attack,
        _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Bilinmeyen PlayerPosition.")
    };

    public static double Multiplier(PlayerPosition playerPosition, PlayerPosition requiredPosition)
    {
        if (playerPosition == requiredPosition)
            return 1.00;

        var playerFamily = FamilyOf(playerPosition);
        var requiredFamily = FamilyOf(requiredPosition);

        if (playerFamily == requiredFamily)
            return 0.70;

        // GK hicbir aileye komsu degil — GK disi bir pozisyonla eslesen GK
        // (veya tam tersi) her zaman "uzak aile" sayilir.
        if (playerFamily == PositionFamily.Goalkeeper || requiredFamily == PositionFamily.Goalkeeper)
            return 0.10;

        var isAdjacent =
            (playerFamily == PositionFamily.Defense && requiredFamily == PositionFamily.Midfield) ||
            (playerFamily == PositionFamily.Midfield && requiredFamily == PositionFamily.Defense) ||
            (playerFamily == PositionFamily.Midfield && requiredFamily == PositionFamily.Attack) ||
            (playerFamily == PositionFamily.Attack && requiredFamily == PositionFamily.Midfield);

        return isAdjacent ? 0.35 : 0.10; // kalan tek olasilik: Defans↔Hucum
    }
}
