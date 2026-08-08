using ClubCraft.MatchEngine.Domain.Enums;
using System.Text.Json.Serialization;

namespace ClubCraft.MatchEngine.Domain.ValueObjects;

public record MatchEvent(int Minute, MatchEventType Type, Guid ClubId, Guid? PlayerId = null)
{
    // Adding parameterless constructor for EF Core JSON serialization if needed
    [JsonConstructor]
    public MatchEvent() : this(0, MatchEventType.Goal, Guid.Empty) { }
}
