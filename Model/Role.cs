using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;

        [JsonIgnore]
        public ICollection<SubforumRole> SubforumRoles { get; set; } = new List<SubforumRole>();
    }
}
