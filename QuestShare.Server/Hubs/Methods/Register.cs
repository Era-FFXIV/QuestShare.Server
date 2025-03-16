using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using QuestShare.Common;
using QuestShare.Server.Hubs;
using QuestShare.Server.Managers;
using QuestShare.Server.Models;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [EnableRateLimiting("ClientPolicy")]
        [HubMethodName(nameof(Register))]
        public async Task Server_Register(Register.Request request)
        {
            if (BanManager.IsBanned(Context))
            {
                Log.Warning($"[REGISTER] Client {Context.ConnectionId} is banned.");
                Context.Abort();
                return;
            }
            var error = Error.None;
            if (request.Version != Constants.Version) error = Error.InvalidVersion;
            var client = await ClientManager.GetClient(Context.ConnectionId, request.Token);
            if (client == null) error = Error.InvalidToken;
            if (client != null)
            {
                var existingSession = await SessionManager.GetSession(client);
                if (existingSession != null) error = Error.AlreadyRegistered;
            }
            if (error != Error.None)
            {
                Log.Warning($"[REGISTER] Client {Context.ConnectionId} failed to register: {error}");
                await Clients.Caller.SendAsync(nameof(Register), new Register.Response { Error = error, Success = false, ShareCode = "" });
                return;
            }
            var session = await SessionManager.GenerateSession(Context.ConnectionId, client!);
            await Clients.Caller.SendAsync(nameof(Register), new Register.Response { Error = Error.None, Success = true, ShareCode = session });
            Log.Information($"[REGISTER] Client {Context.ConnectionId} generated share code {session}.");
        }
    }
}
