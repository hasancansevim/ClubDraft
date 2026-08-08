using ClubCraft.MatchEngine.Domain.ValueObjects;

namespace ClubCraft.MatchEngine.Domain.Entities;

public class Match
{
    public Guid Id { get; private set; }
    public int Week { get; private set; }
    public Guid HomeClubId { get; private set; }
    public Guid AwayClubId { get; private set; }
    
    public int HomeScore { get; private set; }
    public int AwayScore { get; private set; }
    public bool IsPlayed { get; private set; }
    
    private readonly List<MatchEvent> _keyEvents = new();
    public IReadOnlyCollection<MatchEvent> KeyEvents => _keyEvents.AsReadOnly();

    private Match() { } // EF Core

    public Match(Guid id, int week, Guid homeClubId, Guid awayClubId)
    {
        Id = id;
        Week = week;
        HomeClubId = homeClubId;
        AwayClubId = awayClubId;
        IsPlayed = false;
    }

    public void Resolve(int homeScore, int awayScore, IEnumerable<MatchEvent> events)
    {
        if (IsPlayed)
        {
            throw new InvalidOperationException("Match is already played.");
        }

        HomeScore = homeScore;
        AwayScore = awayScore;
        IsPlayed = true;
        
        _keyEvents.AddRange(events);
    }
}
