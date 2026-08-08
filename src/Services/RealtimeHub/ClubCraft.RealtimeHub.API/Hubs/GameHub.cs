using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ClubCraft.RealtimeHub.API.Hubs
{
    public class GameHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var roomId = httpContext?.Request.Query["roomId"];
            var userId = httpContext?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(roomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                // Auth shortcut: In a real app we'd validate the user here. 
                // For now, assume if they have the roomId, they are in the group.
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            var httpContext = Context.GetHttpContext();
            var roomId = httpContext?.Request.Query["roomId"];
            
            if (!string.IsNullOrEmpty(roomId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
