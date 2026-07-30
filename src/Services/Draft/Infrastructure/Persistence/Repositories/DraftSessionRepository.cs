using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.Draft.Application.Repositories;
using ClubCraft.Draft.Domain.Aggregates;
using ClubCraft.Draft.Domain.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.Draft.Infrastructure.Persistence.Repositories;

public class DraftSessionRepository : IDraftSessionRepository
{
    private readonly DraftDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public DraftSessionRepository(DraftDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task AddAsync(DraftSession draftSession, CancellationToken cancellationToken = default)
    {
        await _dbContext.DraftSessions.AddAsync(draftSession, cancellationToken);
    }

    public async Task<DraftSession?> GetByIdAsync(Guid draftSessionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DraftSessions
            .Include(x => x.Picks)
            .Include(x => x.PlayerPool)
            .FirstOrDefaultAsync(x => x.Id == draftSessionId, cancellationToken);
    }

    public async Task SaveAsync(DraftSession draftSession, CancellationToken cancellationToken = default)
    {
        // 1. Extract domain events before saving
        var domainEvents = draftSession.DomainEvents.ToList();
        
        // 2. Clear them so they aren't processed again
        draftSession.ClearDomainEvents();

        // 3. Map to Integration Events and Publish
        // MassTransit EF Core Outbox will intercept these publishes and write them to the OutboxMessage table 
        // as part of the same transaction as DbContext.SaveChangesAsync().
        foreach (var domainEvent in domainEvents)
        {
            switch (domainEvent)
            {
                case PlayerClaimedEvent claimedEvent:
                    await _publishEndpoint.Publish<IPlayerClaimedEvent>(new
                    {
                        PickAttemptId = claimedEvent.PickAttemptId,
                        DraftSessionId = claimedEvent.DraftSessionId,
                        ClubId = claimedEvent.ClubId,
                        PlayerId = claimedEvent.PlayerId,
                        PickNumber = claimedEvent.PickNumber,
                        Name = claimedEvent.Name,
                        Position = claimedEvent.Position,
                        Overall = claimedEvent.Overall,
                        Age = claimedEvent.Age,
                        MarketValue = claimedEvent.MarketValue,
                        OccurredOn = claimedEvent.OccurredOn
                    }, cancellationToken);
                    break;

                case PlayerClaimRevertedEvent revertedEvent:
                    await _publishEndpoint.Publish<IPlayerClaimRevertedEvent>(new
                    {
                        DraftSessionId = revertedEvent.DraftSessionId,
                        PlayerId = revertedEvent.PlayerId,
                        AffectedClubId = revertedEvent.AffectedClubId,
                        OccurredOn = revertedEvent.OccurredOn
                    }, cancellationToken);
                    break;

                case DraftStartedEvent startedEvent:
                    await _publishEndpoint.Publish<IDraftStartedEvent>(new
                    {
                        DraftSessionId = startedEvent.DraftSessionId,
                        OccurredOn = startedEvent.OccurredOn
                    }, cancellationToken);
                    break;

                case DraftTurnAdvancedEvent turnAdvancedEvent:
                    await _publishEndpoint.Publish<IDraftTurnAdvancedEvent>(new
                    {
                        DraftSessionId = turnAdvancedEvent.DraftSessionId,
                        NextClubId = turnAdvancedEvent.NextClubId,
                        PickIndex = turnAdvancedEvent.PickIndex,
                        OccurredOn = turnAdvancedEvent.OccurredOn
                    }, cancellationToken);
                    break;

                case DraftCompletedEvent completedEvent:
                    await _publishEndpoint.Publish<IDraftCompletedEvent>(new
                    {
                        DraftSessionId = completedEvent.DraftSessionId,
                        OccurredOn = completedEvent.OccurredOn
                    }, cancellationToken);
                    break;

                // For other events like PlayerClaimRejectedEvent, we might just log them or broadcast to SignalR, 
                // but if we need them as Integration events, we publish them here similarly.
            }
        }

        // 4. Commit to DB (includes both state changes and outbox messages)
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
