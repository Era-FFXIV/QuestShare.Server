using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using QuestShare.Common;
using QuestShare.Server.Managers;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(GroupLeave))]
        [EnableRateLimiting("ClientPolicy")]
        public async Task Server_GroupLeave(GroupLeave.Request request)
        {
            if (BanManager.IsBanned(Context))
            {
                Context.Abort();
                return;
            }
            var error = Error.None;
            if (request.Token == "") error = Error.InvalidToken;
            else if (request.Version != Common.Constants.Version) error = Error.InvalidVersion;
            var client = await ClientManager.GetClient(Context.ConnectionId);
            if (client == null) error = Error.Unauthorized;
            if (error != Error.None)
            {
                await Clients.Caller.SendAsync(nameof(GroupLeave), new GroupLeave.Response
                {
                    Success = false,
                    Error = error,
                });
                return;
            }
            var session = await SessionManager.GetSession(request.Session.ShareCode);
            if (session == null)
            {
                await Clients.Caller.SendAsync(nameof(GroupLeave), new GroupLeave.Response
                {
                    Success = false,
                    Error = Error.InvalidSession,
                });
                return;
            }
            var smember = await SessionManager.GetMembersInSession(session);
            if (!smember.Any(s => s.Client.ClientId == client!.ClientId))
            {
                await Clients.Caller.SendAsync(nameof(GroupLeave), new GroupLeave.Response
                {
                    Success = false,
                    Error = Error.InvalidMember,
                });
                return;
            }
            await SessionManager.RemoveMemberFromSession(session, client!);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.ShareCode.ToString());
            await ClientManager.RemoveKnownShareCode(client!, session.ShareCode);
            await Clients.Caller.SendAsync(nameof(GroupLeave), new GroupLeave.Response
            {
                Success = true,
                Session = new Objects.Session
                {
                    ShareCode = session.ShareCode,
                    OwnerCharacterId = session.OwnerCharacterId,
                }
            });
            // broadcast to party
            await Clients.GroupExcept(session.SessionId.ToString(), Context.ConnectionId).SendAsync(nameof(GroupLeave.GroupLeaveBroadcast), new GroupLeave.GroupLeaveBroadcast
            {
                Session = new Objects.Session
                {
                    ShareCode = session.ShareCode,
                    OwnerCharacterId = session.OwnerCharacterId,
                }
            });
        }
    }
}
