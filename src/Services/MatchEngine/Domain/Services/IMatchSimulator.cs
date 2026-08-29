using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

public interface IMatchSimulator
{
    void Simulate(Match match, double homePower, double awayPower);
}
