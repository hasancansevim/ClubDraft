using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.ClubManagement.Domain.Enums;
using ClubCraft.ClubManagement.Domain.ValueObjects;

namespace ClubCraft.ClubManagement.Domain.Entities;

public class WeeklyDecision : Entity<Guid>
{
    public int Week { get; private set; }
    public WeeklyDecisionType Type { get; private set; }
    public Money Cost { get; private set; }
    public DateTime DecidedAt { get; private set; }

    private WeeklyDecision() { } // EF Core

    public WeeklyDecision(int week, WeeklyDecisionType type, Money cost)
    {
        Id = Guid.NewGuid();
        Week = week;
        Type = type;
        Cost = cost;
        DecidedAt = DateTime.UtcNow;
    }
}
