using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Tag
    {
        public int TagId { get; set; }
        public string Name { get; set; } = null!;

        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
