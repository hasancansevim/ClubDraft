using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Services;

public interface IMatchSimulator
{
    void Simulate(Match match, int homePower, int awayPower);
}
