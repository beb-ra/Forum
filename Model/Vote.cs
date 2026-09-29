using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Vote
    {
        public int VoteId { get; set; }
        public int UserId { get; set; }
        public int? PostId { get; set; }
        public int? CommentId { get; set; }
        public short VoteType { get; set; }
        public DateTime VoteDate { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public Post? Post { get; set; }
        [JsonIgnore]
        public Comment? Comment { get; set; }
    }
}
