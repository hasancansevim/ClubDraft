namespace ClubCraft.ReputationFan.Domain.Entities;

public class ReputationChange
{
    public int Id { get; private set; } // For EF Core if mapped as entity, or could be Value Object
    public int Delta { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime OccurredAt { get; private set; }

    private ReputationChange() { } // EF Core

    public ReputationChange(int delta, string reason, DateTime occurredAt)
    {
        Delta = delta;
        Reason = reason;
        OccurredAt = occurredAt;
    }
}
