using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.ReputationFan.Application.Repositories;
using ClubCraft.ReputationFan.Domain.Aggregates;
using MassTransit;

namespace ClubCraft.ReputationFan.Application.Consumers;

public class WeeklyDecisionMadeEventConsumer : IConsumer<IWeeklyDecisionMadeEvent>
{
    private readonly IClubReputationRepository _repository;

    public WeeklyDecisionMadeEventConsumer(IClubReputationRepository repository)
    {
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<IWeeklyDecisionMadeEvent> context)
    {
        var msg = context.Message;
        
        // 1 = StadiumInvestment (from WeeklyDecisionType enum)
        if (msg.Type == 1)
        {
            var rep = await _repository.GetByIdAsync(msg.ClubId) ?? new ClubReputation(msg.ClubId);
            rep.AddReputation(3, $"Stadium Upgraded (Week {msg.Week})");
            await _repository.SaveAsync(rep);
        }
    }
}
