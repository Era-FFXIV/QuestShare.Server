using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using QuestShare.Server.Managers;
using Microsoft.AspNetCore.Http.Features;
using QuestShare.Server.Models;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Log.Information($"Client connected: {Context.ConnectionId}");
            if (BanManager.IsBanned(Context))
            {
                Log.Warning($"Client {Context.ConnectionId} is banned.");
                Context.Abort();
            }
            Clients.Caller.SendAsync(nameof(AuthRequest), new AuthRequest.Response
            {
            });
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Log.Information($"Client {Context.ConnectionId} disconnected.");
            if (exception != null)
            {
                Log.Error(exception, "Exception occurred on disconnect.");
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
