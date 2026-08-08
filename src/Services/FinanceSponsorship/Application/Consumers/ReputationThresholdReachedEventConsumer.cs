using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.FinanceSponsorship.Application.Repositories;
using ClubCraft.FinanceSponsorship.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClubCraft.FinanceSponsorship.Application.Consumers;

public class ReputationThresholdReachedEventConsumer : IConsumer<IReputationThresholdReachedEvent>
{
    private readonly ISponsorshipOfferRepository _repository;
    private readonly ILogger<ReputationThresholdReachedEventConsumer> _logger;

    public ReputationThresholdReachedEventConsumer(
        ISponsorshipOfferRepository repository,
        ILogger<ReputationThresholdReachedEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IReputationThresholdReachedEvent> context)
    {
        var message = context.Message;
        
        try
        {
            var offer = new SponsorshipOffer(
                Guid.NewGuid(),
                message.ClubId,
                message.Threshold,
                message.Threshold * 10000m,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7)
            );

            await _repository.AddAsync(offer, context.CancellationToken);
            _logger.LogInformation("Created sponsorship offer for club {ClubId} reaching threshold {Threshold}", message.ClubId, message.Threshold);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_SponsorshipOffer") == true)
        {
            _logger.LogWarning("Sponsorship offer for club {ClubId} and threshold {Threshold} already exists. Ignoring.", message.ClubId, message.Threshold);
        }
    }
}
