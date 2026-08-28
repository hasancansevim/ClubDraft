using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Repositories;
using ClubCraft.MatchEngine.Domain.Entities;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class PlayerAddedToRosterCommandConsumer : IConsumer<IPlayerAddedToRosterEvent>
{
    private readonly IClubPowerRatingRepository _powerRepository;

    public PlayerAddedToRosterCommandConsumer(IClubPowerRatingRepository powerRepository)
    {
        _powerRepository = powerRepository;
    }

    public async Task Consume(ConsumeContext<IPlayerAddedToRosterEvent> context)
    {
        var msg = context.Message;
        
        var power = await _powerRepository.GetByIdAsync(msg.ClubId, context.CancellationToken);
        if (power == null)
        {
            // Lazily create if not exists
            power = new ClubPowerRating(msg.ClubId, msg.RoomId);
        }

        power.AddPlayer(msg.Overall);
        
        await _powerRepository.SaveAsync(power, context.CancellationToken);
    }
}
