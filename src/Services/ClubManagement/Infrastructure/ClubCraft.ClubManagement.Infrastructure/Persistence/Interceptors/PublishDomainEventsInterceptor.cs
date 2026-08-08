using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.ClubManagement.Domain.Events;
using ClubCraft.BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClubCraft.ClubManagement.Infrastructure.Persistence.Interceptors;

public class PublishDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PublishDomainEventsInterceptor(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;

        if (dbContext is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var entitiesWithEvents = dbContext.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        entitiesWithEvents.ForEach(e => e.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await PublishIntegrationEventAsync(domainEvent, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishIntegrationEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case PlayerAddedToRosterEvent e:
                await _publishEndpoint.Publish<IPlayerAddedToRosterEvent>(new
                {
                    ClubId = e.ClubId,
                    PlayerId = e.PlayerId,
                    Overall = e.Overall,
                    PickAttemptId = e.PickAttemptId
                }, cancellationToken);
                break;
            case PlayerRosterAdditionFailedEvent e:
                await _publishEndpoint.Publish<IPlayerRosterAdditionFailedEvent>(new
                {
                    ClubId = e.ClubId,
                    PlayerId = e.PlayerId,
                    PickAttemptId = e.PickAttemptId,
                    Reason = e.Reason
                }, cancellationToken);
                break;
            case PlayerRemovedFromRosterEvent e:
                await _publishEndpoint.Publish<IPlayerRemovedFromRosterEvent>(new
                {
                    ClubId = e.ClubId,
                    PlayerId = e.PlayerId
                }, cancellationToken);
                break;
            case WeeklyDecisionMadeEvent e:
                await _publishEndpoint.Publish<IWeeklyDecisionMadeEvent>(new
                {
                    ClubId = e.ClubId,
                    Week = e.Week,
                    Type = (int)e.Type,
                    Cost = e.Cost
                }, cancellationToken);
                break;
        }
    }
}
