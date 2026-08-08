using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.ReputationFan.Application.Repositories;
using ClubCraft.ReputationFan.Domain.Aggregates;
using MassTransit;

namespace ClubCraft.ReputationFan.Application.Consumers;

public class PlayerAddedToRosterEventConsumer : IConsumer<IPlayerAddedToRosterEvent>
{
    private readonly IClubReputationRepository _repository;

    public PlayerAddedToRosterEventConsumer(IClubReputationRepository repository)
    {
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<IPlayerAddedToRosterEvent> context)
    {
        var msg = context.Message;
        var rep = await _repository.GetByIdAsync(msg.ClubId) ?? new ClubReputation(msg.ClubId);

        if (msg.Overall >= 85)
        {
            rep.AddReputation(5, $"Star Transfer Bonus: Player {msg.PlayerId} (Overall: {msg.Overall})");
        }
        else if (msg.Overall >= 75)
        {
            rep.AddReputation(1, $"Good Transfer: Player {msg.PlayerId} (Overall: {msg.Overall})");
        }

        await _repository.SaveAsync(rep);
    }
}
