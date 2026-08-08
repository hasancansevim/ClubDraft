using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Services;
using MediatR;

namespace ClubCraft.MatchEngine.Application.Commands.GenerateFixture;

public class GenerateFixtureCommandHandler : IRequestHandler<GenerateFixtureCommand>
{
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureGenerator _fixtureGenerator;

    public GenerateFixtureCommandHandler(IFixtureRepository fixtureRepository, IFixtureGenerator fixtureGenerator)
    {
        _fixtureRepository = fixtureRepository;
        _fixtureGenerator = fixtureGenerator;
    }

    public async Task Handle(GenerateFixtureCommand request, CancellationToken cancellationToken)
    {
        var existingFixture = await _fixtureRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);
        if (existingFixture != null)
        {
            // Fixture already generated
            return;
        }

        var fixture = _fixtureGenerator.GenerateFixture(request.RoomId, request.ClubIds);
        await _fixtureRepository.SaveAsync(fixture, cancellationToken);
    }
}
