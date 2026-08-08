using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.MatchEngine.Domain.Entities;

namespace ClubCraft.MatchEngine.Domain.Aggregates;

public class Fixture : AggregateRoot<Guid>
{
    public Guid RoomId { get; private set; }
    
    private readonly List<Match> _matches = new();
    public IReadOnlyCollection<Match> Matches => _matches.AsReadOnly();

    private Fixture() { }

    public Fixture(Guid id, Guid roomId, IEnumerable<Match> matches)
    {
        Id = id;
        RoomId = roomId;
        _matches.AddRange(matches);
    }
    
    public IEnumerable<Match> GetMatchesForWeek(int week)
    {
        return _matches.Where(m => m.Week == week);
    }
}
