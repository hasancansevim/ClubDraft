using System;
using System.Threading.Tasks;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.Session.Domain.Aggregates;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ClubCraft.Session.Infrastructure.Persistence;

namespace ClubCraft.Session.API.Consumers
{
    public class DraftCompletedEventConsumer : IConsumer<IDraftCompletedEvent>
    {
        private readonly IServiceProvider _serviceProvider;

        public DraftCompletedEventConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<IDraftCompletedEvent> context)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SessionDbContext>();

            var gameRoom = await dbContext.GameRooms
                .Include(g => g.Participants)
                .FirstOrDefaultAsync(g => g.Id == context.Message.DraftSessionId);

            if (gameRoom is not null)
            {
                gameRoom.AdvanceToSeason();
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
