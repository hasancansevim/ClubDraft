namespace ClubCraft.Draft.Application.Commands.ClaimPlayer;

public class ClaimPlayerResult
{
    public bool Success { get; private set; }
    public int? PickNumber { get; private set; }
    public string? Reason { get; private set; }

    private ClaimPlayerResult(bool success, int? pickNumber, string? reason)
    {
        Success = success;
        PickNumber = pickNumber;
        Reason = reason;
    }

    public static ClaimPlayerResult IsSuccess(int pickNumber)
    {
        return new ClaimPlayerResult(true, pickNumber, null);
    }

    public static ClaimPlayerResult Fail(string reason)
    {
        return new ClaimPlayerResult(false, null, reason);
    }
}
