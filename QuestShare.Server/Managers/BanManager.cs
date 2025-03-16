using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using QuestShare.Server.Models;
using System.Net;

namespace QuestShare.Server.Managers
{
    public static class BanManager
    {
        public static void Ban(HubCallerContext context, string reason, ulong characterId = 0)
        {
            using var db = new QuestShareContext();
            var ip = GetIp(context)!;
            db.Bans.Add(new Ban
            {
                BanIp = ip,
                BanCharacterId = characterId,
                BanReason = reason,
                BanIssuer = "Server",
                BanDate = DateTime.UtcNow,
                BanExpiry = DateTime.UtcNow.AddHours(1)
            });
            Console.WriteLine($"Banned {ip} for {reason}");
        }
        public static bool IsBanned(HubCallerContext context, ulong characterId = 0)
        {
            using var db = new QuestShareContext();
            var ip = GetIp(context)!;
            return db.Bans.Any(b => (b.BanIp == ip || b.BanCharacterId == characterId) && b.BanExpiry > DateTime.UtcNow);
        }

        public static IPAddress? GetIp(HubCallerContext context)
        {
            var feature = context.Features.Get<IHttpConnectionFeature>();
            var httpContext = context.GetHttpContext();
            if (httpContext == null) return feature!.RemoteIpAddress!;
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var value))
            {
                return IPAddress.Parse(value!);
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Real-IP", out value))
            {
                return IPAddress.Parse(value!);
            }
            else if (httpContext.Request.Headers.TryGetValue("CF-Connecting-IP", out value))
            {
                return IPAddress.Parse(value!);
            }
            else
            {
                return feature!.RemoteIpAddress;
            }
        }

        public static bool CheckBadRequests(HubCallerContext context, string requestType)
        {
            var requests = context.Items.TryGetValue(requestType, out var value) ? (int)value! : 0;
            if (requests > 5)
            {
                Ban(context, $"Excessive bad {requestType} requests.");
                return true;
            } else
            {
                context.Items[requestType] = requests + 1;
                return false;
            }
        }
    }
}
