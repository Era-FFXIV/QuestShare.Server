using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using QuestShare.Common;

namespace QuestShare.Server.Models
{
    public class SessionMember
    {
        public Guid ClientSessionId { get; set; }
        public virtual required Client Client { get; set; }
        public virtual required Session Session { get; set; }
       // public required string CharacterId { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    internal class ClientSessionConfiguration : IEntityTypeConfiguration<SessionMember>
    {
        public void Configure(EntityTypeBuilder<SessionMember> builder)
        {
            builder.ToTable("SessionMembers");
            builder.HasKey(cs => cs.ClientSessionId);
            builder.HasMany<Client>();
            builder.HasMany<Session>();
            builder.Property(cs => cs.Created).HasDefaultValueSql("current_timestamp").ValueGeneratedOnAddOrUpdate();
            builder.Property(cs => cs.LastUpdated).HasDefaultValueSql("current_timestamp").ValueGeneratedOnAddOrUpdate();
        }
    }
}
