using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QuestShare.Server.Models
{
    public class Client
    {
        [Key]
        public Guid ClientId { get; set; }
        public required string ConnectionId { get; set; } = null!;
        public required string Token { get; set; } = null!;
        public List<string> KnownShareCodes { get; set; } = [];

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime Created { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUpdated { get; set; }
    }

}
