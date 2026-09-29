using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Post : ISoftDelete
    {
        public int PostId { get; set; }
        public int SubforumId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreationDate { get; set; }
        public int AuthorId { get; set; }
        public int Rating { get; set; }
        public int? TagId { get; set; }
        public bool IsPinned { get; set; }
        public bool IsClosed { get; set; }
        public bool IsDeleted { get; set; }

        [JsonIgnore]
        public Subforum Subforum { get; set; } = null!;
        [JsonIgnore]
        public User Author { get; set; } = null!;
        [JsonIgnore]
        public Tag? Tag { get; set; }
        [JsonIgnore]
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        [JsonIgnore]
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
