using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using QuestShare.Common;

namespace QuestShare.Server.Models
{
    public class Session
    {
        [Key]
        public Guid SessionId { get; set; }
        public string OwnerCharacterId { get; set; } = "";
        public virtual required Client Owner { get; set; }
        public required string ShareCode { get; set; }
        public required string ReservedConnectionId { get; set; }
        public int SharedQuestId { get; set; } = 0;
        public byte SharedQuestStep { get; set; } = 0;
        public List<string> PartyMembers { get; set; } = [];
        public bool IsActive { get; set; } = true;
        public bool SkipPartyCheck { get; set; } = false;
        public bool AllowJoins { get; set; } = true;
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }

        public bool IsMember(string characterId)
        {
            return PartyMembers.Contains(characterId);
        }
        public void AddMember(string characterId)
        {
            PartyMembers.Add(characterId);
        }
        public void RemoveMember(string characterId)
        {
            PartyMembers.Remove(characterId);
        }

    }

    public class SessionsConfiguration : IEntityTypeConfiguration<Session>
    {
        public void Configure(EntityTypeBuilder<Session> builder)
        {
            builder.ToTable("Sessions");
            builder.Property(s => s.Created).HasDefaultValueSql("current_timestamp").ValueGeneratedOnAddOrUpdate(); ;
            builder.Property(s => s.LastUpdated).HasDefaultValueSql("current_timestamp").ValueGeneratedOnAddOrUpdate();
            builder.Property(s => s.PartyMembers).HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<List<string>>(v ?? "")!
            );
        }
    }
}
