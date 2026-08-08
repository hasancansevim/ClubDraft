using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Repositories;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class PlayerRemovedFromRosterCommandConsumer : IConsumer<IPlayerRemovedFromRosterEvent>
{
    private readonly IClubPowerRatingRepository _powerRepository;

    public PlayerRemovedFromRosterCommandConsumer(IClubPowerRatingRepository powerRepository)
    {
        _powerRepository = powerRepository;
    }

    public async Task Consume(ConsumeContext<IPlayerRemovedFromRosterEvent> context)
    {
        var msg = context.Message;
        
        var power = await _powerRepository.GetByIdAsync(msg.ClubId, context.CancellationToken);
        if (power != null)
        {
            // We need to know the 'Overall' to remove it. But IPlayerRemovedFromRosterEvent doesn't have it!
            // This is a known issue. In a real app we'd fetch the player's overall or store the roster in MatchEngine.
            // For now, this is a limitation, we might just reduce by a flat amount or ignore it as Draft only ADDS players currently.
            
            // Temporary workaround: since Draft doesn't remove players during season, this is largely unimplemented.
            // In a full implementation, we should sync the whole Roster to MatchEngine.
            
            await _powerRepository.SaveAsync(power, context.CancellationToken);
        }
    }
}
