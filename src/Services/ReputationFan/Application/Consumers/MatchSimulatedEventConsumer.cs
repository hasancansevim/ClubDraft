using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.ReputationFan.Application.Repositories;
using ClubCraft.ReputationFan.Domain.Aggregates;
using MassTransit;

namespace ClubCraft.ReputationFan.Application.Consumers;

public class MatchSimulatedEventConsumer : IConsumer<IMatchSimulatedEvent>
{
    private readonly IClubReputationRepository _repository;

    public MatchSimulatedEventConsumer(IClubReputationRepository repository)
    {
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<IMatchSimulatedEvent> context)
    {
        var msg = context.Message;

        var homeRep = await _repository.GetByIdAsync(msg.HomeClubId) ?? new ClubReputation(msg.HomeClubId);
        var awayRep = await _repository.GetByIdAsync(msg.AwayClubId) ?? new ClubReputation(msg.AwayClubId);

        if (msg.HomeScore > msg.AwayScore)
        {
            homeRep.AddReputation(3, $"Match Won vs Club {msg.AwayClubId} (Week {msg.Week})");
            awayRep.AddReputation(-1, $"Match Lost vs Club {msg.HomeClubId} (Week {msg.Week})");
        }
        else if (msg.HomeScore < msg.AwayScore)
        {
            homeRep.AddReputation(-1, $"Match Lost vs Club {msg.AwayClubId} (Week {msg.Week})");
            awayRep.AddReputation(3, $"Match Won vs Club {msg.HomeClubId} (Week {msg.Week})");
        }
        else
        {
            homeRep.AddReputation(1, $"Match Drawn vs Club {msg.AwayClubId} (Week {msg.Week})");
            awayRep.AddReputation(1, $"Match Drawn vs Club {msg.HomeClubId} (Week {msg.Week})");
        }

        foreach (var evt in homeRep.DomainEvents) 
        {
            if (evt is IReputationThresholdReachedEvent thresholdEvent)
                await context.Publish<IReputationThresholdReachedEvent>(thresholdEvent);
            else
                await context.Publish((object)evt);
        }
        homeRep.ClearDomainEvents();
        await _repository.SaveAsync(homeRep);

        foreach (var evt in awayRep.DomainEvents) 
        {
            if (evt is IReputationThresholdReachedEvent thresholdEvent)
                await context.Publish<IReputationThresholdReachedEvent>(thresholdEvent);
            else
                await context.Publish((object)evt);
        }
        awayRep.ClearDomainEvents();
        await _repository.SaveAsync(awayRep);
    }
}
