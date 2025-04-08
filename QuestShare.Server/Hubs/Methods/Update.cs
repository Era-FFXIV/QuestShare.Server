using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using QuestShare.Common;
using QuestShare.Server.Managers;
using QuestShare.Server.Models;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(Update))]
        [EnableRateLimiting("ClientPolicy")]
        public async Task Server_Update(Update.Request request)
        {
            Log.Debug($"[UPDATE] Client {Context.ConnectionId} requested update.");
            if (BanManager.IsBanned(Context))
            {
                Context.Abort();
                return;
            }
            var error = Error.None;
            var client = await ClientManager.GetClient(Context.ConnectionId);
            if (client == null) error = Error.Unauthorized;
            else if (request.Token == "" || request.Token != client.Token) error = Error.InvalidToken;
            else if (request.Version != Common.Constants.Version) error = Error.InvalidVersion;
            var session = await SessionManager.GetSession(request.Session.ShareCode);
            if (error == Error.None)
            {
                if (session == null) error = Error.InvalidSession;
            }
            if (error != Error.None)
            {
                Log.Warning($"[UPDATE] Client {Context.ConnectionId} failed update with error {error}.");
                await Clients.Caller.SendAsync(nameof(Update), new Update.Response
                {
                    Success = false,
                    Error = error,
                });
                return;
            }
            await SessionManager.SetPartyMembers(session!.ShareCode, [.. request.PartyMembers]);
            
            if (request.IsQuestUpdate)
            {
                await SessionManager.UpdateActiveQuest(request.Session.ShareCode, request.Session.ActiveQuestId, request.Session.ActiveQuestStep);
                // Broadcast to party
                await Clients.GroupExcept(session.ShareCode.ToString(), Context.ConnectionId).SendAsync(nameof(Update.UpdateBroadcast), new Update.UpdateBroadcast
                {
                    Session = request.Session.Session,
                });
            } else
            {
                await SessionManager.SetSessionSettings(session.ShareCode, request.Session);
                await Clients.Caller.SendAsync(nameof(Update), new Update.Response
                {
                    Success = true,
                    Error = Error.None,
                });
            }
        }
    }
}
