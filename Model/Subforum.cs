using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Subforum : ISoftDelete
    {
        public int SubforumId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        [JsonIgnore]
        public ICollection<Ban> Bans { get; set; } = new List<Ban>();
        [JsonIgnore]
        public ICollection<SubforumRole> SubforumRoles { get; set; } = new List<SubforumRole>();
    }
}
