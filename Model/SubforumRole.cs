using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class SubforumRole
    {
        public int SubfRoleId { get; set; }
        public int UserId { get; set; }
        public int? SubforumId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public Subforum? Subforum { get; set; }
        [JsonIgnore]
        public Role Role { get; set; } = null!;
    }
}
