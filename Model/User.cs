using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace ForumWebAPI.Model
{
    public class User : ISoftDelete
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        [JsonIgnore]
        public string PasswordHash { get; set; } = null!;
        public DateTime RegistrationDate { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        [JsonIgnore]
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        [JsonIgnore]
        public ICollection<Ban> ReceivedBans { get; set; } = new List<Ban>();
        [JsonIgnore]
        public ICollection<Ban> IssuedBans { get; set; } = new List<Ban>();
        [JsonIgnore]
        public ICollection<SubforumRole> SubforumRoles { get; set; } = new List<SubforumRole>();
    }
}
