using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.MatchEngine.Application.Repositories;
using MassTransit;

namespace ClubCraft.MatchEngine.Application.Consumers;

public class WeeklyDecisionMadeEventConsumer : IConsumer<IWeeklyDecisionMadeEvent>
{
    private readonly IClubPowerRatingRepository _powerRepository;

    public WeeklyDecisionMadeEventConsumer(IClubPowerRatingRepository powerRepository)
    {
        _powerRepository = powerRepository;
    }

    public async Task Consume(ConsumeContext<IWeeklyDecisionMadeEvent> context)
    {
        var msg = context.Message;
        
        var power = await _powerRepository.GetByIdAsync(msg.ClubId, context.CancellationToken);
        if (power == null)
            return; // Can't apply bonus if club not found

        // Simple mapping: Type 1 (Training) = +5 MoraleBonus, Type 2 (TeamBuilding) = +10 MoraleBonus
        int bonus = msg.Type switch
        {
            1 => 5,
            2 => 10,
            _ => 0
        };

        if (bonus > 0)
        {
            power.ApplyMoraleBonus(bonus);
            await _powerRepository.SaveAsync(power, context.CancellationToken);
        }
    }
}
