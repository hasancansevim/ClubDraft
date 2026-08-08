using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Services;
using MediatR;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Commands.SimulateMatchesForWeek;

public class SimulateMatchesForWeekCommandHandler : IRequestHandler<SimulateMatchesForWeekCommand>
{
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IClubPowerRatingRepository _powerRepository;
    private readonly IMatchSimulator _matchSimulator;
    private readonly IPublishEndpoint _publishEndpoint;

    public SimulateMatchesForWeekCommandHandler(IFixtureRepository fixtureRepository, IClubPowerRatingRepository powerRepository, IMatchSimulator matchSimulator, IPublishEndpoint publishEndpoint)
    {
        _fixtureRepository = fixtureRepository;
        _powerRepository = powerRepository;
        _matchSimulator = matchSimulator;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(SimulateMatchesForWeekCommand request, CancellationToken cancellationToken)
    {
        var fixture = await _fixtureRepository.GetByRoomIdAsync(request.RoomId, cancellationToken);
        if (fixture == null)
            throw new Exception($"Fixture for room {request.RoomId} not found.");

        var matchesForWeek = fixture.Matches.Where(m => m.Week == request.Week && !m.IsPlayed).ToList();

        foreach (var match in matchesForWeek)
        {
            var homePower = await _powerRepository.GetByIdAsync(match.HomeClubId, cancellationToken);
            var awayPower = await _powerRepository.GetByIdAsync(match.AwayClubId, cancellationToken);

            if (homePower == null || awayPower == null)
            {
                // Can't simulate without power rating. Depending on business rules, could assume 0 or throw.
                throw new Exception($"Missing power rating for clubs {match.HomeClubId} or {match.AwayClubId}");
            }

            _matchSimulator.Simulate(match, homePower.ComputedPower, awayPower.ComputedPower);

            // Reset Morale Bonus after playing
            homePower.ResetMoraleBonus();
            awayPower.ResetMoraleBonus();

            await _publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IMatchSimulatedEvent>(new
            {
                MatchId = match.Id,
                RoomId = fixture.RoomId,
                Week = match.Week,
                HomeClubId = match.HomeClubId,
                AwayClubId = match.AwayClubId,
                HomeScore = match.HomeScore,
                AwayScore = match.AwayScore
            }, cancellationToken);

            await _powerRepository.SaveAsync(homePower, cancellationToken);
            await _powerRepository.SaveAsync(awayPower, cancellationToken);
        }

        await _publishEndpoint.Publish<ClubCraft.BuildingBlocks.Contracts.Events.IWeekSimulationCompletedEvent>(new
        {
            RoomId = fixture.RoomId,
            Week = request.Week
        }, cancellationToken);

        await _fixtureRepository.SaveAsync(fixture, cancellationToken);
    }
}
