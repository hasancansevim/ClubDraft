using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

public interface IClubPowerCalculator
{
    ClubPowerBreakdown Calculate(ClubPowerRating rating);
}

/// <summary>
/// TakimGucu formulunun ara degerleri de dahil tam sonucu (testte/hata
/// ayiklamada "neden bu sayi" sorusuna cevap verebilmek icin).
/// TakimGucu = BazGuc + YildizBonusu + DerinlikBonusu + MoralModifiye.
/// (Ev sahibi +%5 avantaji bunun USTUNE, MatchSimulator'da uygulanir —
/// buraya dahil degildir.)
/// </summary>
public record ClubPowerBreakdown(
    double BazGuc,
    double YildizBonusu,
    double DerinlikBonusu,
    int MoralModifiye,
    double TeamPower);
