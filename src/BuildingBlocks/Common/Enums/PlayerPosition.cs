using System.Text.Json.Serialization;

namespace ClubCraft.BuildingBlocks.Common.Enums;

/// <summary>
/// Oyuncunun sahadaki detayli mevkii — kaba kategori (GK/DEF/MID/FWD) yerine
/// gercek FIFA/EA FC pozisyon kodlari (process_players.py'nin CSV'den okudugu
/// "player_positions" alaninin birincil kodu). Draft.Domain (PlayerSnapshot) ve
/// ClubManagement.Domain (Player) BUNU paylasiyor — ayni oyuncu draft'tan
/// roster'a gecerken pozisyonun her iki bounded context'te de ayni anlami
/// tasimasi gerektigi icin BuildingBlocks.Common'da tek bir tanim tutuluyor
/// (iki ayri enum'un elle senkron tutulma riskinden kacinmak icin).
///
/// [JsonStringEnumConverter]: WeeklyDecisionType'ta yasanan servisler-arasi
/// int-degeri karisikligi (bkz. spec.md, 2026-08-28 notu) burada tekrarlanmasin
/// diye JSON'da HER ZAMAN string olarak (orn. "CB", "CDM") serialize/deserialize
/// edilir — frontend zaten position alanini opak bir string olarak isliyor,
/// bu yuzden frontend'de degisiklik gerekmiyor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayerPosition
{
    GK,
    CB,
    RB,
    LB,
    RWB,
    LWB,
    CDM,
    CM,
    CAM,
    RM,
    LM,
    RW,
    LW,
    ST,
    CF
}
