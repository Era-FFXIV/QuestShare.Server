using Microsoft.AspNetCore.SignalR;
using QuestShare.Common;
using QuestShare.Server.Managers;

namespace QuestShare.Server.Hubs
{

    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(Cancel))]
        public async Task Server_Cancel(Cancel.Request request)
        {
            if (BanManager.IsBanned(Context))
            {
                Context.Abort();
                return;
            }
            var client = await ClientManager.GetClient(Context.ConnectionId);
            if (client == null)
            {
                await Clients.Caller.SendAsync(nameof(Cancel), new Cancel.Response
                {
                    Success = false,
                    Error = Error.Unauthorized,
                });
                return;
            }
            var error = Error.None;
            if (request.Token == "") error = Error.InvalidToken;
            else if (request.Version != Common.Constants.Version) error = Error.InvalidVersion;
            var session = await SessionManager.GetSession(request.ShareCode);
            if (session == null) error = Error.Unauthorized;
            if (session != null && session.OwnerCharacterId != request.OwnerCharacterId) error = Error.InvalidSession;
            if (session != null && session.Owner.ClientId != client.ClientId) error = Error.Unauthorized;
            if (error != Error.None)
            {
                await Clients.Caller.SendAsync(nameof(Cancel), new Cancel.Response
                {
                    Success = false,
                    Error = error,
                });
                return;
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, session!.ShareCode.ToString());
            var members = await ClientManager.GetClientsInSession(session);
            if (members.Count > 0)
            {
                // broadcast to party
                await Clients.GroupExcept(session.ShareCode.ToString(), Context.ConnectionId).SendAsync(nameof(Cancel.CancelBroadcast), new Cancel.CancelBroadcast
                {
                    ShareCode = request.ShareCode
                });
                foreach (var member in members)
                {
                    await Groups.RemoveFromGroupAsync(member.Client.ConnectionId, session.ShareCode.ToString());
                    await ClientManager.RemoveClientSession(member.Client);
                }
            }
            await SessionManager.RemoveSession(session!.ShareCode);
            await Clients.Caller.SendAsync(nameof(Cancel), new Cancel.Response
            {
                Success = true,
            });
            
        }
    }
}
