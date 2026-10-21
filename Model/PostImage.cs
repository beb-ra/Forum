using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class PostImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public int PostId { get; set; }

        [JsonIgnore]
        public Post Post { get; set; } = null!;
    }
}
