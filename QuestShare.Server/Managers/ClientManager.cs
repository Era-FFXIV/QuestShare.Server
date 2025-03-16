using Microsoft.EntityFrameworkCore;
using QuestShare.Server.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace QuestShare.Server.Managers
{
    public static class ClientManager
    {
        public static async Task<Client> AddClient(string connectionId)
        {
            using var context = new QuestShareContext();
            var token = GenerateToken();
            var c = context.Clients.Add(new Client
            {
                ConnectionId = connectionId,
                Token = token
            });
            await context.SaveChangesAsync();
            return c.Entity;
        }
        public static async Task RemoveClient(string connectionId)
        {
            using var context = new QuestShareContext();
            var client = context.Clients.FirstOrDefault(c => c.ConnectionId == connectionId);
            if (client != null)
            {
                context.Clients.Remove(client);
            }
            await context.SaveChangesAsync();
        }
        public static async Task<Client?> GetClient(string connectionId, string token = "")
        {
            using var context = new QuestShareContext();
            var client = await context.Clients.Where(c => c.ConnectionId == connectionId || c.Token == token).FirstOrDefaultAsync();
            if (client != null)
            {
                if (token != "" && client.Token != token)
                {
                    Log.Warning($"[ClientManager] Found a client, but tokens did not match. {token} != {client.Token}");
                    return null;
                }
                if (client.ConnectionId != connectionId)
                {
                    Log.Information($"[ClientManager] Changing connection ID from {connectionId} to {client.ConnectionId} for token {token}");
                    await ChangeClientConnectionId(client.ConnectionId, connectionId);
                }
                return client;
            } else
            {
                Log.Warning($"[ClientManager] Unable to find client for {connectionId} or token {token}");
                return null;
            }
        }

        public static async Task<List<SessionMember>> GetClientsInSession(Session session)
        {
            using var context = new QuestShareContext();
            var clients = await context.SessionMembers.Where(c => c.Session == session).Include(s => s.Session).Include(sm => sm.Client).ToListAsync();
            return clients;
        }

        public static async Task RemoveClientSession(Client client)
        {
            using var context = new QuestShareContext();
            var cs = await context.SessionMembers.Where(cs => cs.Client.ClientId == client.ClientId).FirstOrDefaultAsync();
            if (cs == null)
            {
                Log.Warning($"[ClientManager] Unable to find client session for {client.ClientId}");
                return;
            }
            context.SessionMembers.Remove(cs);
            Log.Debug($"[ClientManager] Removing client {client.ClientId} from session");
            await context.SaveChangesAsync();
        }

        public static async Task AddClientSession(Guid ClientId, Guid SessionId)
        {
            using var context = new QuestShareContext();
            var client = await context.Clients.Where(c => c.ClientId == ClientId).FirstOrDefaultAsync();
            var session = await context.Sessions.Where(s => s.SessionId == SessionId).FirstOrDefaultAsync();
            if (client == null || session == null)
            {
                Log.Warning($"[ClientManager] Unable to find client {ClientId} or session {SessionId}");
                return;
            }
            await context.SessionMembers.AddAsync(new SessionMember
            {
                Client = client,
                Session = session
            });
            Log.Debug($"[ClientManager] Adding client {client.ClientId} to session {session.SessionId}");
            await context.SaveChangesAsync();
        }

        public static async Task ChangeClientConnectionId(string oldConnectionId, string newConnectionId)
        {
            using var context = new QuestShareContext();
            var client = await context.Clients.Where(c => c.ConnectionId == oldConnectionId).FirstOrDefaultAsync();
            if (client != null)
            {
                client.ConnectionId = newConnectionId;
                await context.SaveChangesAsync();
            }
        }

        public static async Task AddKnownShareCode(Client client, string shareCode)
        {
            using var context = new QuestShareContext();
            var c = await context.Clients.Where(c => c.ClientId == client.ClientId).FirstOrDefaultAsync();
            if (c != null)
            {
                c.KnownShareCodes.Add(shareCode);
                await context.SaveChangesAsync();
            }
        }

        public static async Task RemoveKnownShareCode(Client client, string shareCode)
        {
            using var context = new QuestShareContext();
            var c = await context.Clients.Where(c => c.ClientId == client.ClientId).FirstOrDefaultAsync();
            if (c != null)
            {
                c.KnownShareCodes.Remove(shareCode);
                await context.SaveChangesAsync();
            }
        }

        public static string GenerateToken()
        {
            var random = RandomNumberGenerator.GetHexString(32, true);
            return random;
        }
    }
}
