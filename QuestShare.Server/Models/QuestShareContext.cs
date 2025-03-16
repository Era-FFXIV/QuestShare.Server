using Microsoft.EntityFrameworkCore;
using QuestShare.Common.API;
using Serilog.Events;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace QuestShare.Server.Models
{
    public class QuestShareContext : DbContext
    {
        public QuestShareContext()
        {
           // Database.EnsureCreated();
        }
        public DbSet<Session> Sessions { get; set; } = null!;
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Ban> Bans { get; set; } = null!;
        public DbSet<SessionMember> SessionMembers { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseNpgsql(Environment.GetEnvironmentVariable("QUESTSHARE_DATABASE"));

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Client>().Property(a => a.Created).HasDefaultValueSql("current_timestamp");
            modelBuilder.Entity<Client>().Property(a => a.LastUpdated).HasDefaultValueSql("current_timestamp").ValueGeneratedOnAddOrUpdate();
            modelBuilder.Entity<Client>().Property(a => a.KnownShareCodes).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );
            new SessionsConfiguration().Configure(modelBuilder.Entity<Session>());
            new ClientSessionConfiguration().Configure(modelBuilder.Entity<SessionMember>());
            modelBuilder.Entity<Ban>().ToTable("Bans");
        }
    }
}
