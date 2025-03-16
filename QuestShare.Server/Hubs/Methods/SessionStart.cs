using Microsoft.AspNetCore.SignalR;
using QuestShare.Common;
using QuestShare.Server.Managers;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(SessionStart))]
        public async Task Server_SessionStart(SessionStart.Request request)
        {
            if (BanManager.IsBanned(Context))
            {
                Log.Error($"[AUTHORIZE] Client {Context.ConnectionId} is banned.");
                Context.Abort();
                return;
            }
            var error = Error.None;
            var client = await ClientManager.GetClient(Context.ConnectionId, request.Token);
            if (client == null) error = Error.Unauthorized;
            var session = await SessionManager.GetSession(request.Session.Session.ShareCode);
            if (session == null) error = Error.InvalidSession;
            else
            {
                if (Context.ConnectionId != session.ReservedConnectionId)
                {
                    error = Error.InvalidSession;
                }
            }
            if (error != Error.None)
            {
                await Clients.Caller.SendAsync(nameof(SessionStart), new SessionStart.Response
                {
                    Error = error,
                    Success = false
                });
                return;
            }
            var s = await SessionManager.CreateSession(client!, Context.ConnectionId, request.Session);
            if (s == null)
            {
                Log.Error($"[SessionStart] Unable to create session for {client!.Token} and {request.Session.Session.ShareCode}");
                await Clients.Caller.SendAsync(nameof(SessionStart), new SessionStart.Response
                {
                    Error = Error.InternalServerError,
                    Success = false
                });
            } else {
                Log.Debug($"[SessionStart] Created session {s.ShareCode} for {client!.Token}");
                var sObject = new Objects.Session
                {
                    OwnerCharacterId = s.OwnerCharacterId,
                    ShareCode = s.ShareCode,
                };
                var ownedSession = new Objects.OwnedSession
                {
                    IsActive = s.IsActive,
                    SkipPartyCheck = s.SkipPartyCheck,
                    AllowJoins = s.AllowJoins,
                    Session = sObject,
                };
                await Clients.Caller.SendAsync(nameof(SessionStart), new SessionStart.Response
                {
                    Error = Error.None,
                    Success = true,
                    Session = ownedSession
                });
            }
        }
    }
}
