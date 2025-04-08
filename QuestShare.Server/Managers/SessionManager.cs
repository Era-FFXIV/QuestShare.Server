using Microsoft.EntityFrameworkCore;
using QuestShare.Common;
using QuestShare.Server.Models;

namespace QuestShare.Server.Managers
{
    public class SessionManager
    {
        public static async Task<Session?> GetSession(Client client)
        {
            using var context = new QuestShareContext();
            return await context.Sessions.Where(s => s.Owner.ClientId == client.ClientId).Include(session => session.Owner).FirstOrDefaultAsync();
        }
        public static async Task<Session?> GetSession(string ShareCode)
        {
            using var context = new QuestShareContext();
            var session = await context.Sessions.Where(s => s.ShareCode == ShareCode).Include(session => session.Owner).FirstOrDefaultAsync();
            return session;
        }

        public static async Task<int> GetSessionCount()
        {
            using var context = new QuestShareContext();
            var sessionCount = await context.Sessions.Where(s => s.LastUpdated > s.Created && s.LastUpdated >= DateTime.UtcNow.AddHours(-1)).CountAsync();
            return sessionCount;
        }

        public static async Task<string> GenerateSession(string connectionId, Client client)
        {
            using var context = new QuestShareContext();
            var token = ClientManager.GenerateToken();
            var c = await context.Clients.Where(c => c.ClientId == client.ClientId).FirstOrDefaultAsync();
            var s = context.Sessions.Add(new Session
            {
                ShareCode = token[..8].ToUpperInvariant(),
                ReservedConnectionId = connectionId,
                Owner = c!,
            });
            await context.SaveChangesAsync();
            return token[..8].ToUpperInvariant();
        }

        public static async Task<Session?> CreateSession(Client owner, string connectionId, Objects.OwnedSession session)
        {
            using var context = new QuestShareContext();
            var s = await context.Sessions.Where(s => s.ShareCode == session.Session.ShareCode && s.ReservedConnectionId == connectionId).FirstOrDefaultAsync();
            if (s == null)
            {
                Log.Error($"[SessionManager] Failed to create session for {session.Session.OwnerCharacterId} with share code {session.Session.ShareCode}");
                return null;
            }
            s.OwnerCharacterId = session.Session.OwnerCharacterId;
            s.IsActive = session.IsActive;
            s.AllowJoins = session.AllowJoins;
            s.SkipPartyCheck = session.SkipPartyCheck;
            await context.SaveChangesAsync();
            await AddMemberToSession(s, s.OwnerCharacterId);
            return s;
        }

        public static async Task RemoveSession(string shareCode)
        {
            using var context = new QuestShareContext();
            var session = await context.Sessions.Where(s => s.ShareCode == shareCode).FirstOrDefaultAsync();
            if (session != null)
            {
                context.Sessions.Remove(session);
                await context.SaveChangesAsync();
            }
        }

        public static async Task SetPartyMembers(string shareCode, List<string> partyMembers)
        {
            using var context = new QuestShareContext();
            var s = await context.Sessions.Where(s => s.ShareCode == shareCode).FirstOrDefaultAsync();
            if (s != null)
            {
                s.PartyMembers = partyMembers;
                await context.SaveChangesAsync();
            }
        }

        public static async Task AddMemberToSession(Session session, string member)
        {
            using var context = new QuestShareContext();
            var s = await context.Sessions.Where(s => s.SessionId == session.SessionId).FirstOrDefaultAsync();
            if (s != null)
            {
                s.AddMember(member);
                await context.SaveChangesAsync();
            }
        }

        public static async Task RemoveMemberFromSession(Session session, Client client)
        {
            using var context = new QuestShareContext();
            await context.SessionMembers.Where(s => s.Session.SessionId == session.SessionId && s.Client.ClientId == client.ClientId).ExecuteDeleteAsync();
        }

        public static async Task<List<SessionMember>> GetMembersInSession(Session session)
        {
            using var context = new QuestShareContext();
            var s = await context.SessionMembers.Where(s => s.Session.SessionId == session.SessionId).Include(sm => sm.Session).Include(sm => sm.Client).ToListAsync();
            return s;
        }

        public static async Task UpdateActiveQuest(string shareCode, int questId, byte questStep)
        {
            using var context = new QuestShareContext();
            var session = await context.Sessions.Where(s => s.ShareCode == shareCode).FirstOrDefaultAsync();
            if (session == null)
            {
                // log error
                Console.Error.WriteLine($"Failed to update quests for session {shareCode}");
                return;
            }
            session.SharedQuestStep = questStep;
            session.SharedQuestId = questId;
            var records = await context.SaveChangesAsync();
            Log.Debug($"[UPDATE] Updated {records} quests for session {shareCode}");
        }

        public static async Task SetSessionSettings(string shareCode, Objects.OwnedSession sessionObj)
        {
            using var context = new QuestShareContext();
            var session = await context.Sessions.Where(s => s.ShareCode == shareCode).FirstOrDefaultAsync();
            if (session == null)
            {
                // log error
                Console.Error.WriteLine($"Failed to update settings for session {shareCode}");
                return;
            }
            session.IsActive = sessionObj.IsActive;
            session.AllowJoins = sessionObj.AllowJoins;
            session.SkipPartyCheck = sessionObj.SkipPartyCheck;
            var records = await context.SaveChangesAsync();
            Log.Debug($"[UPDATE] Updated {records} settings for session {shareCode}");
        }
    }
}
