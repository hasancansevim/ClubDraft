namespace ClubCraft.MatchEngine.Domain.Entities;

public class ClubPowerRating
{
    public Guid ClubId { get; private set; }
    public Guid RoomId { get; private set; }
    public int TotalOverall { get; private set; }
    public int MoraleBonus { get; private set; }
    public int ComputedPower => TotalOverall + MoraleBonus;

    private ClubPowerRating() { }

    public ClubPowerRating(Guid clubId, Guid roomId)
    {
        ClubId = clubId;
        RoomId = roomId;
        TotalOverall = 0;
        MoraleBonus = 0;
    }

    public void AddPlayer(int overall)
    {
        TotalOverall += overall;
    }

    public void RemovePlayer(int overall)
    {
        TotalOverall -= overall;
        if (TotalOverall < 0) TotalOverall = 0;
    }

    public void ApplyMoraleBonus(int bonus)
    {
        MoraleBonus += bonus;
    }

    public void ResetMoraleBonus()
    {
        MoraleBonus = 0;
    }
}
