using System.Net;

namespace QuestShare.Server.Models
{
    public class Ban
    {
        public string BanId { get; set; } = null!;
        public IPAddress BanIp { get; set; } = null!;
        public ulong BanCharacterId { get; set; } = 0;
        public string BanReason { get; set; } = null!;
        public string BanIssuer { get; set; } = null!;
        public DateTime BanDate { get; set; }
        public DateTime BanExpiry { get; set; }
    }
}
