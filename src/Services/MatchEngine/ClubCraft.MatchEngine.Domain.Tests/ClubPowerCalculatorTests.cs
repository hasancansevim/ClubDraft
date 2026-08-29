using ClubCraft.BuildingBlocks.Common.Enums;
using ClubCraft.MatchEngine.Domain.Entities;
using ClubCraft.MatchEngine.Domain.Enums;
using ClubCraft.MatchEngine.Domain.Services;
using Xunit;
using Xunit.Abstractions;

namespace ClubCraft.MatchEngine.Domain.Tests;

/// <summary>
/// Match Engine guc formulu derinlestirmesi icin regresyon testleri
/// (spec.md, "Match Engine Guc Formulu Derinlestirme"). Ayni desen:
/// MatchEngineDomainTests.MatchSimulator_WeightedRandom_StrongerTeamWinsMoreOften
/// ile tutarli (seed'li simulator, dongu, _output.WriteLine ile istatistik).
/// </summary>
public class ClubPowerCalculatorTests
{
    private readonly ITestOutputHelper _output;
    private readonly IClubPowerCalculator _calculator = new ClubPowerCalculator();

    public ClubPowerCalculatorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private sealed record SquadPlayer(Guid Id, PlayerPosition Position, int Overall);

    /// <summary>
    /// 20 kisilik gercekci bir kadro: Ilk 11'e (4-4-2) tam oturan 11 oyuncu
    /// (index 0-10, formasyon slot sirasiyla ayni sirada) + 9 kisilik cesitli
    /// pozisyonlu bir yedek kulubesi (index 11-19).
    /// </summary>
    private static List<SquadPlayer> BuildSquad() => new()
    {
        // Ilk 11 (4-4-2 slot sirasiyla: GK,LB,CB1,CB2,RB,LM,CM1,CM2,RM,ST1,ST2)
        new(Guid.NewGuid(), PlayerPosition.GK, 80),
        new(Guid.NewGuid(), PlayerPosition.LB, 74),
        new(Guid.NewGuid(), PlayerPosition.CB, 78),
        new(Guid.NewGuid(), PlayerPosition.CB, 76),
        new(Guid.NewGuid(), PlayerPosition.RB, 73),
        new(Guid.NewGuid(), PlayerPosition.LM, 72),
        new(Guid.NewGuid(), PlayerPosition.CM, 79),
        new(Guid.NewGuid(), PlayerPosition.CM, 75),
        new(Guid.NewGuid(), PlayerPosition.RM, 71),
        new(Guid.NewGuid(), PlayerPosition.ST, 85),
        new(Guid.NewGuid(), PlayerPosition.ST, 77),
        // Yedek kulubesi
        new(Guid.NewGuid(), PlayerPosition.GK, 65),
        new(Guid.NewGuid(), PlayerPosition.CB, 70),
        new(Guid.NewGuid(), PlayerPosition.LB, 68),
        new(Guid.NewGuid(), PlayerPosition.RB, 66),
        new(Guid.NewGuid(), PlayerPosition.CDM, 69),
        new(Guid.NewGuid(), PlayerPosition.CAM, 72),
        new(Guid.NewGuid(), PlayerPosition.RW, 70),
        new(Guid.NewGuid(), PlayerPosition.LW, 68),
        new(Guid.NewGuid(), PlayerPosition.CF, 66),
    };

    private static ClubPowerRating BuildRating(List<SquadPlayer> squad, Dictionary<string, Guid?> slots)
    {
        var rating = new ClubPowerRating(Guid.NewGuid(), Guid.NewGuid());
        foreach (var p in squad)
            rating.AddPlayer(p.Id, p.Overall, p.Position);
        rating.UpdateLineup("4-4-2", slots);
        return rating;
    }

    private static Dictionary<string, Guid?> CorrectLineup(List<SquadPlayer> squad) => new()
    {
        ["GK"] = squad[0].Id,
        ["LB"] = squad[1].Id,
        ["CB1"] = squad[2].Id,
        ["CB2"] = squad[3].Id,
        ["RB"] = squad[4].Id,
        ["LM"] = squad[5].Id,
        ["CM1"] = squad[6].Id,
        ["CM2"] = squad[7].Id,
        ["RM"] = squad[8].Id,
        ["ST1"] = squad[9].Id,
        ["ST2"] = squad[10].Id,
    };

    /// <summary>
    /// Bilerek bozuk bir dizilim: kaleci (squad[0]) forvete, bir forvet
    /// (squad[9]) kaleye konuyor — GK'nin klasik "kaleciyi forvette oynat"
    /// ornegi. Ayrica bir stoper (squad[2]) diger forvetle (squad[10])
    /// yer degistiriyor, boylece dizilim bir tek slotluk tesadufe degil,
    /// gercekten "kadro tamamen yanlis dizilmis" bir senaryoya karsilik
    /// geliyor.
    /// </summary>
    private static Dictionary<string, Guid?> BrokenLineup(List<SquadPlayer> squad)
    {
        var lineup = CorrectLineup(squad);
        lineup["GK"] = squad[9].Id;
        lineup["ST1"] = squad[0].Id;
        lineup["CB1"] = squad[10].Id;
        lineup["ST2"] = squad[2].Id;
        return lineup;
    }

    [Fact]
    public void PositionCompatibility_Multiplier_MatchesSpecTable()
    {
        // Tam eslesme -> 1.00
        Assert.Equal(1.00, PositionCompatibility.Multiplier(PlayerPosition.CB, PlayerPosition.CB));
        Assert.Equal(1.00, PositionCompatibility.Multiplier(PlayerPosition.GK, PlayerPosition.GK));

        // Ayni aile, farkli pozisyon -> 0.85
        Assert.Equal(0.85, PositionCompatibility.Multiplier(PlayerPosition.CB, PlayerPosition.RB));
        Assert.Equal(0.85, PositionCompatibility.Multiplier(PlayerPosition.CM, PlayerPosition.CDM));
        Assert.Equal(0.85, PositionCompatibility.Multiplier(PlayerPosition.ST, PlayerPosition.RW));

        // Komsu aile (Defans<->OrtaSaha, OrtaSaha<->Hucum) -> 0.65
        Assert.Equal(0.65, PositionCompatibility.Multiplier(PlayerPosition.CB, PlayerPosition.CM));
        Assert.Equal(0.65, PositionCompatibility.Multiplier(PlayerPosition.CM, PlayerPosition.ST));

        // Uzak aile (Defans<->Hucum, GK<->herhangi biri) -> 0.40
        Assert.Equal(0.40, PositionCompatibility.Multiplier(PlayerPosition.CB, PlayerPosition.ST));
        Assert.Equal(0.40, PositionCompatibility.Multiplier(PlayerPosition.GK, PlayerPosition.CB));
        Assert.Equal(0.40, PositionCompatibility.Multiplier(PlayerPosition.GK, PlayerPosition.ST));
    }

    [Fact]
    public void Calculate_BrokenLineup_ScoresLowerThanCorrectLineup()
    {
        var squad = BuildSquad();
        var correct = _calculator.Calculate(BuildRating(squad, CorrectLineup(squad)));
        var broken = _calculator.Calculate(BuildRating(squad, BrokenLineup(squad)));

        _output.WriteLine($"Dogru dizilim   -> TeamPower={correct.TeamPower:F2}  (BazGuc={correct.BazGuc:F2}, YildizBonusu={correct.YildizBonusu:F2}, DerinlikBonusu={correct.DerinlikBonusu:F2})");
        _output.WriteLine($"Bozuk dizilim   -> TeamPower={broken.TeamPower:F2}  (BazGuc={broken.BazGuc:F2}, YildizBonusu={broken.YildizBonusu:F2}, DerinlikBonusu={broken.DerinlikBonusu:F2})");
        _output.WriteLine($"Fark: {correct.TeamPower - broken.TeamPower:F2}");

        Assert.True(broken.TeamPower < correct.TeamPower,
            "Bozuk dizilim (kaleci forvette vb.), ayni kadroyla dogru diziliminden daha yuksek/esit guc uretmemeli.");
        Assert.True(correct.TeamPower - broken.TeamPower > 8,
            $"Fark cok kucuk ({correct.TeamPower - broken.TeamPower:F2}), pozisyon carpaninin etkisi beklenenden zayif.");
    }

    [Fact]
    public void Simulate_BrokenLineup_ScoresFewerPointsThanCorrectLineupOverManyMatches()
    {
        var squad = BuildSquad();
        var correctRating = BuildRating(squad, CorrectLineup(squad));
        var brokenRating = BuildRating(squad, BrokenLineup(squad));

        // Sabit bir rakip: ayni guc dagilimina sahip, dogru dizilmis bagimsiz bir kadro.
        // Dogru dizilimli kulup bu rakiple esit gucte oldugu icin sonuc ~yari yariya
        // beklenir (sürpriz payi korunuyor); bozuk dizilimli kulup ise aynı rakibe
        // karsi belirgin sekilde daha az puan almali.
        var opponentSquad = BuildSquad();
        var opponentRating = BuildRating(opponentSquad, CorrectLineup(opponentSquad));

        const int iterations = 30;
        var correctSeries = SimulateSeries(correctRating, opponentRating, iterations, seed: 7);
        var brokenSeries = SimulateSeries(brokenRating, opponentRating, iterations, seed: 7);

        _output.WriteLine($"--- {iterations} maclik seri, ayni sabit rakibe karsi ---");
        _output.WriteLine($"Dogru dizilim: {correctSeries.Wins}G {correctSeries.Draws}B {correctSeries.Losses}M, toplam {correctSeries.Points} puan");
        _output.WriteLine($"Bozuk dizilim: {brokenSeries.Wins}G {brokenSeries.Draws}B {brokenSeries.Losses}M, toplam {brokenSeries.Points} puan");

        Assert.True(brokenSeries.Points < correctSeries.Points,
            "Bozuk dizilimli kadro, ayni rakibe karsi ayni sayida macta dogru diziliminden daha az puan almali.");
    }

    private sealed record SeriesResult(int Points, int Wins, int Draws, int Losses);

    private SeriesResult SimulateSeries(ClubPowerRating club, ClubPowerRating opponent, int iterations, int seed)
    {
        var simulator = new MatchSimulator(seed);
        var clubPower = _calculator.Calculate(club).TeamPower;
        var opponentPower = _calculator.Calculate(opponent).TeamPower;

        int points = 0, wins = 0, draws = 0, losses = 0;
        for (int i = 0; i < iterations; i++)
        {
            // Ev sahibi avantajinin iki tarafi da esit sikliklada etkilemesi icin don nobetlese.
            bool clubIsHome = i % 2 == 0;
            var homeId = clubIsHome ? club.ClubId : opponent.ClubId;
            var awayId = clubIsHome ? opponent.ClubId : club.ClubId;
            var match = new Match(Guid.NewGuid(), 1, homeId, awayId);

            var homePower = clubIsHome ? clubPower : opponentPower;
            var awayPower = clubIsHome ? opponentPower : clubPower;
            simulator.Simulate(match, homePower, awayPower);

            var clubScore = clubIsHome ? match.HomeScore : match.AwayScore;
            var opponentScore = clubIsHome ? match.AwayScore : match.HomeScore;

            if (clubScore > opponentScore) { points += 3; wins++; }
            else if (clubScore == opponentScore) { points += 1; draws++; }
            else { losses++; }
        }

        return new SeriesResult(points, wins, draws, losses);
    }

    [Fact]
    public void ApplyMatchResult_Moral_ClampsAtBounds()
    {
        var winRating = new ClubPowerRating(Guid.NewGuid(), Guid.NewGuid());
        winRating.ApplyMatchResult(MatchOutcome.Win);
        Assert.Equal(3, winRating.Moral);
        winRating.ApplyMatchResult(MatchOutcome.Win);
        Assert.Equal(5, winRating.Moral); // 3+3=6 -> 5'te sinirlanir
        winRating.ApplyMatchResult(MatchOutcome.Win);
        Assert.Equal(5, winRating.Moral); // ucuncu galibiyette de 5'te kilitli kalir

        var lossRating = new ClubPowerRating(Guid.NewGuid(), Guid.NewGuid());
        lossRating.ApplyMatchResult(MatchOutcome.Loss);
        Assert.Equal(-3, lossRating.Moral);
        lossRating.ApplyMatchResult(MatchOutcome.Loss);
        Assert.Equal(-5, lossRating.Moral);
        lossRating.ApplyMatchResult(MatchOutcome.Loss);
        Assert.Equal(-5, lossRating.Moral);

        var drawRating = new ClubPowerRating(Guid.NewGuid(), Guid.NewGuid());
        drawRating.ApplyMatchResult(MatchOutcome.Win); // Moral=3
        drawRating.ApplyMatchResult(MatchOutcome.Draw); // 0'a 1 birim yaklasir -> 2
        Assert.Equal(2, drawRating.Moral);
        drawRating.ApplyMatchResult(MatchOutcome.Loss); // Moral=-1
        Assert.Equal(-1, drawRating.Moral);
        drawRating.ApplyMatchResult(MatchOutcome.Draw); // 0'a 1 birim yaklasir -> 0
        Assert.Equal(0, drawRating.Moral);
        drawRating.ApplyMatchResult(MatchOutcome.Draw); // zaten 0, degismez
        Assert.Equal(0, drawRating.Moral);
    }

    [Fact]
    public void Calculate_RosterUnder11Players_Throws()
    {
        var rating = new ClubPowerRating(Guid.NewGuid(), Guid.NewGuid());
        rating.AddPlayer(Guid.NewGuid(), 75, PlayerPosition.GK);

        Assert.Throws<InvalidOperationException>(() => _calculator.Calculate(rating));
    }
}
