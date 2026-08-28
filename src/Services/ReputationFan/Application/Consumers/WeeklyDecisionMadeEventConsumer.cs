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
        
        // WeeklyDecisionType enum: HireCoach=1, StadiumInvestment=2, MoraleBonus=3.
        // Onceden yanlislikla "1 = StadiumInvestment" saniliyordu (asil HireCoach'a karsilik
        // geliyordu) — spec.md'ye gore stadyum yatirimi itibari artirmali (bkz. §3.4), bu yuzden
        // dogru deger 2.
        if (msg.Type == 2)
        {
            var rep = await _repository.GetByIdAsync(msg.ClubId) ?? new ClubReputation(msg.ClubId);
            rep.AddReputation(3, $"Stadium Upgraded (Week {msg.Week})");

            // AddReputation, esik asildiginda ReputationThresholdReachedEvent'i aggregate'in
            // domain event listesine ekliyor — burada okuyup publish etmezsek event sessizce kaybolur
            // (bkz. MatchSimulatedEventConsumer'daki dogru desen).
            foreach (var evt in rep.DomainEvents)
            {
                if (evt is IReputationThresholdReachedEvent thresholdEvent)
                    await context.Publish<IReputationThresholdReachedEvent>(thresholdEvent);
                else
                    await context.Publish((object)evt);
            }
            rep.ClearDomainEvents();

            await _repository.SaveAsync(rep);
        }
    }
}
