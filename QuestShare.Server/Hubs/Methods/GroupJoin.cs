using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using QuestShare.Common;
using QuestShare.Server.Managers;
using QuestShare.Server.Models;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(GroupJoin))]
        public async Task Server_GroupJoin(GroupJoin.Request request)
        {
            var error = Error.None;
            if (request.Token == "") error = Error.InvalidToken;
            if (request.Version != Common.Constants.Version) error = Error.InvalidVersion;
            var client = await ClientManager.GetClient(Context.ConnectionId, request.Token);
            if (client == null) error = Error.Unauthorized;
            if (error != Error.None)
            {
                await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
                {
                    Success = false,
                    Error = error,
                });
                return;
            }
            var session = await SessionManager.GetSession(request.SessionInfo.Code);
            if (session == null)
            {
                Log.Warning($"[GroupJoin] Session {request.SessionInfo.Code} not found.");
                await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
                {
                    Success = false,
                    Error = Error.InvalidParty,
                });
                return;
            }
            if (!session.AllowJoins)
            {
                Log.Warning($"[GroupJoin] Session {request.SessionInfo.Code} does not allow joins.");
                await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
                {
                    Success = false,
                    Error = Error.InvalidParty,
                });
                return;
            }
            if (session.Owner.ClientId == client!.ClientId)
            {
                Log.Warning($"[GroupJoin] Client {client} is the owner of session {session.ShareCode}");
                await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
                {
                    Success = false,
                    Error = Error.InvalidParty,
                });
                return;
            }
            if (!session.SkipPartyCheck && !session.PartyMembers.Contains(request.SessionInfo.CharacterId))
            {
                Log.Warning($"[GroupJoin] Client {client} is not joined to party hosted by {session.OwnerCharacterId}.");
                await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
                {
                    Success = false,
                    Error = Error.InvalidParty,
                });
                return;
            }
            await ClientManager.AddClientSession(client!.ClientId, session.SessionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, session.ShareCode.ToString());
            await ClientManager.AddKnownShareCode(client!, session.ShareCode);
            await Clients.Caller.SendAsync(nameof(GroupJoin), new GroupJoin.Response
            {
                Success = true,
                Session = new Objects.Session
                {
                    OwnerCharacterId = session.OwnerCharacterId,
                    ShareCode = session.ShareCode,
                    ActiveQuestId = session.SharedQuestId,
                    ActiveQuestStep = session.SharedQuestStep
                },
            });
            await Clients.GroupExcept(Context.ConnectionId, session.ShareCode.ToString()).SendAsync(nameof(GroupJoin.GroupJoinBroadcast), new GroupJoin.GroupJoinBroadcast
            {
                Session = new Objects.Session
                {
                    OwnerCharacterId = session.OwnerCharacterId,
                    ShareCode = session.ShareCode,
                },
            });
            Log.Debug($"[GroupJoin] {client} joined session {request.SessionInfo.Code}");
        }
    }
}
