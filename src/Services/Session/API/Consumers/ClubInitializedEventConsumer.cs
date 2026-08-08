using MassTransit;
using Microsoft.EntityFrameworkCore;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.Session.Infrastructure.Persistence;

namespace ClubCraft.Session.API.Consumers;

public class ClubInitializedEventConsumer : IConsumer<IClubInitializedEvent>
{
    private readonly SessionDbContext _dbContext;

    public ClubInitializedEventConsumer(SessionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<IClubInitializedEvent> context)
    {
        var gameRoom = await _dbContext.GameRooms
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.Id == context.Message.RoomId);

        if (gameRoom == null) return;

        var participant = gameRoom.Participants.FirstOrDefault(p => p.Id == context.Message.ParticipantId);
        if (participant == null) return;

        // Idempotency Check
        if (participant.ClubId.HasValue) return;

        participant.AssignClub(context.Message.ClubId);
        await _dbContext.SaveChangesAsync();
    }
}
