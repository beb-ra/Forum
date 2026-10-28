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
        public bool IsPinned { get; set; }
        public string? ImageUrl { get; set; }

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

        /*
        public static double CalculateScore(int commentRating, int replyCount, 
            int totalRepliesRating, DateTime creationDate, bool isDeleted, bool isPinned)
        {
            var order = Math.Log10(Math.Max(Math.Abs(commentRating), 1));

            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var seconds = (creationDate.ToUniversalTime() - epoch).TotalSeconds;

            var replyBonus = Math.Log10(replyCount + 1) * 1.25;

            var discussionBonus = Math.Log10(Math.Max(Math.Abs(totalRepliesRating), 1)) * 2;

            var deletedBonus = isDeleted ? -5 : 0;

            var pinnedBonus = isPinned ? 100000 : 0;

            return order + seconds / 45000.0 + replyBonus + discussionBonus + deletedBonus + pinnedBonus;
        }
        */
    }
}
