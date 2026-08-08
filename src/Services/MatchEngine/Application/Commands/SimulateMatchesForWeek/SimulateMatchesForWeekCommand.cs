using MediatR;

namespace ClubCraft.MatchEngine.Application.Commands.SimulateMatchesForWeek;

public record SimulateMatchesForWeekCommand(Guid RoomId, int Week) : IRequest;
