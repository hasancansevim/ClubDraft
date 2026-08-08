using MediatR;

namespace ClubCraft.MatchEngine.Application.Commands.GenerateFixture;

public record GenerateFixtureCommand(Guid RoomId, List<Guid> ClubIds) : IRequest;
