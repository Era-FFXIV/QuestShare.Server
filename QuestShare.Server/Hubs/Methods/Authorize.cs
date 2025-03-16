using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuestShare.Common;
using QuestShare.Server.Managers;

namespace QuestShare.Server.Hubs
{
    public partial class ShareHub : Hub
    {
        [HubMethodName(nameof(Authorize))]
        public async Task Server_Authorize(Authorize.Request request)
        {
            if (BanManager.IsBanned(Context))
            {
                Log.Error($"[AUTHORIZE] Client {Context.ConnectionId} is banned.");
                Context.Abort();
                return;
            }
            var error = Error.None;
            Log.Debug($"[AUTHORIZE] Client {Context.ConnectionId} attempting to authorize. {JsonConvert.SerializeObject(request)}");
            if (request.Version != Common.Constants.Version) error = Error.InvalidVersion;
            if (error != Error.None)
            {
                Log.Warning($"[AUTHORIZE] Client {Context.ConnectionId} failed authorization with error {error}.");
                await Clients.Caller.SendAsync(nameof(Authorize), new Authorize.Response
                {
                    Success = false,
                    Error = error,
                });
                Context.Abort();
                return;
            }
            var client = await ClientManager.GetClient(Context.ConnectionId, request.Token);
            if (client == null)
            {
                // create new client
                client = await ClientManager.AddClient(Context.ConnectionId);
                Context.Items.Add("Token", client.Token);
                Log.Information($"[AUTHORIZE] Client {Context.ConnectionId} authorized with token {client.Token}.");

            }
            else
            {
                Log.Information($"[AUTHORIZE] Client {Context.ConnectionId} reauthorized with token {client.Token}.");
                Context.Items.Add("Token", client.Token);
                await ClientManager.ChangeClientConnectionId(client.ConnectionId, Context.ConnectionId);
            }
            var sessions = new List<Objects.Session>();
            Objects.OwnedSession? ownedSession = null;

            foreach (var share in request.ShareCodes)
            {
                var session = await SessionManager.GetSession(share.Code);
                if (session != null)
                {
                    var members = await ClientManager.GetClientsInSession(session);
                    if (session.OwnerCharacterId == share.CharacterId && client.ClientId == session.Owner.ClientId && ownedSession == null)
                    {
                        ownedSession = new Objects.OwnedSession
                        {
                            Session = new Objects.Session
                            {
                                OwnerCharacterId = session.OwnerCharacterId,
                                ShareCode = session.ShareCode,
                                ActiveQuestId = session.SharedQuestId,
                                ActiveQuestStep = session.SharedQuestStep
                            },
                            SkipPartyCheck = session.SkipPartyCheck,
                            IsActive = session.IsActive,
                            AllowJoins = session.AllowJoins,
                        };
                    }
                    else if (members.Any(m => m.Client.ClientId == client.ClientId))
                    {
                        sessions.Add(new Objects.Session
                        {
                            OwnerCharacterId = session.OwnerCharacterId,
                            ActiveQuestId = session.SharedQuestId,
                            ActiveQuestStep = session.SharedQuestStep,
                            ShareCode = share.Code,
                        });
                        await Groups.AddToGroupAsync(Context.ConnectionId, session.ShareCode);
                    }
                    else
                    {
                        sessions.Add(new Objects.Session
                        {
                            OwnerCharacterId = "",
                            ActiveQuestId = 0,
                            ActiveQuestStep = 0,
                            ShareCode = share.Code,
                            IsValid = false,
                        });
                    }
                }
                else
                {
                    sessions.Add(new Objects.Session
                    {
                        OwnerCharacterId = "",
                        ActiveQuestId = 0,
                        ActiveQuestStep = 0,
                        ShareCode = share.Code,
                        IsValid = false,
                    });
                }
            }
            await Clients.Caller.SendAsync(nameof(Authorize), new Authorize.Response
            {
                Success = true,
                Error = Error.None,
                Token = client.Token,
                Sessions = sessions,
                OwnedSession = ownedSession,
            });
        }
    }
}
