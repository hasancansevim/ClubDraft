using ClubCraft.ClubManagement.Application.Repositories;
using ClubCraft.ClubManagement.Domain.Aggregates;
using ClubCraft.BuildingBlocks.Common.SeedWork;
using ClubCraft.ClubManagement.Domain.Events;
using ClubCraft.BuildingBlocks.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ClubCraft.ClubManagement.Infrastructure.Persistence;

public class ClubRepository : IClubRepository
{
    private readonly ClubDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;

    public ClubRepository(ClubDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Club?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clubs
            .Include(c => c.Roster)
            .Include(c => c.WeeklyDecisions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Club?> GetByParticipantIdAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clubs
            .Include(c => c.Roster)
            .Include(c => c.WeeklyDecisions)
            .FirstOrDefaultAsync(x => x.ParticipantId == participantId, cancellationToken);
    }

    public async Task SaveAsync(Club club, CancellationToken cancellationToken = default)
    {
        var entry = _dbContext.Entry(club);
        if (entry.State == EntityState.Detached)
        {
            _dbContext.Clubs.Add(club);
        }

        var domainEvents = club.DomainEvents.ToList();
        club.ClearDomainEvents();

        foreach (var domainEvent in domainEvents)
        {
            await PublishIntegrationEventAsync(domainEvent, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishIntegrationEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case ClubInitializedEvent e:
                await _publishEndpoint.Publish<IClubInitializedEvent>(new
                {
                    ParticipantId = e.ParticipantId,
                    ClubId = e.ClubId,
                    RoomId = e.RoomId
                }, cancellationToken);
                break;
            case PlayerAddedToRosterEvent e:
                await _publishEndpoint.Publish<IPlayerAddedToRosterEvent>(new
                {
                    ClubId = e.ClubId,
                    RoomId = e.RoomId,
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
