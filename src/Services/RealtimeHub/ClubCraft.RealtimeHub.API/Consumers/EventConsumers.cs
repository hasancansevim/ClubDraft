using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MassTransit;
using ClubCraft.BuildingBlocks.Contracts.Events;
using ClubCraft.RealtimeHub.API.Hubs;

namespace ClubCraft.RealtimeHub.API.Consumers
{
    public class EventConsumers :
        IConsumer<IRoomCreatedEvent>,
        IConsumer<IParticipantJoinedEvent>,
        IConsumer<IParticipantReadyEvent>,
        IConsumer<IAllParticipantsReadyForDraftEvent>,
        IConsumer<IAllParticipantsReadyForNextWeekEvent>,
        IConsumer<IPlayerClaimedEvent>,
        IConsumer<IDraftTurnAdvancedEvent>,
        IConsumer<IDraftCompletedEvent>,
        IConsumer<IMatchSimulatedEvent>,
        IConsumer<IWeekSimulationCompletedEvent>
    {
        private readonly IHubContext<GameHub> _hubContext;

        public EventConsumers(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<IRoomCreatedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onRoomCreated", context.Message);
        }

        public async Task Consume(ConsumeContext<IParticipantJoinedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onParticipantJoined", context.Message);
        }

        public async Task Consume(ConsumeContext<IParticipantReadyEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onParticipantReady", context.Message);
        }

        public async Task Consume(ConsumeContext<IAllParticipantsReadyForDraftEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onDraftReady", context.Message);
        }

        public async Task Consume(ConsumeContext<IAllParticipantsReadyForNextWeekEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onWeekAdvanceReady", context.Message);
        }

        public async Task Consume(ConsumeContext<IPlayerClaimedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.DraftSessionId.ToString())
                .SendAsync("onPlayerClaimed", context.Message);
        }

        public async Task Consume(ConsumeContext<IDraftTurnAdvancedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.DraftSessionId.ToString())
                .SendAsync("onDraftTurnAdvanced", context.Message);
        }

        public async Task Consume(ConsumeContext<IDraftCompletedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.DraftSessionId.ToString())
                .SendAsync("onDraftCompleted", context.Message);
        }

        public async Task Consume(ConsumeContext<IMatchSimulatedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onMatchResult", context.Message);
        }

        public async Task Consume(ConsumeContext<IWeekSimulationCompletedEvent> context)
        {
            await _hubContext.Clients.Group(context.Message.RoomId.ToString())
                .SendAsync("onWeekAdvanced", context.Message);
        }
    }
}
