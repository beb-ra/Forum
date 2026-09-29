using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Comment : ISoftDelete
    {
        public int CommentId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreationDate { get; set; }
        public int AuthorId { get; set; }
        public int PostId { get; set; }
        public int? ParentCommentId { get; set; }
        public bool IsDeleted { get; set; }
        public int Rating { get; set; }

        [JsonIgnore]
        public User Author { get; set; } = null!;
        [JsonIgnore]
        public Post Post { get; set; } = null!;
        [JsonIgnore]
        public Comment? ParentComment { get; set; }
        [JsonIgnore]
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        [JsonIgnore]
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
