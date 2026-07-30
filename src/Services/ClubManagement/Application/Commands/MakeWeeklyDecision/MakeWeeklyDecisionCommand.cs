using MediatR;
using ClubCraft.ClubManagement.Domain.Enums;

namespace ClubCraft.ClubManagement.Application.Commands.MakeWeeklyDecision;

public record MakeWeeklyDecisionCommand(Guid ClubId, int Week, WeeklyDecisionType Type) : IRequest<MakeWeeklyDecisionResult>;

public class MakeWeeklyDecisionResult
{
    public bool Success { get; }
    public string? Reason { get; }
    public decimal? Cost { get; }

    private MakeWeeklyDecisionResult(bool success, string? reason, decimal? cost)
    {
        Success = success;
        Reason = reason;
        Cost = cost;
    }

    public static MakeWeeklyDecisionResult IsSuccess(decimal cost) => new(true, null, cost);
    public static MakeWeeklyDecisionResult Fail(string reason) => new(false, reason, null);
}
