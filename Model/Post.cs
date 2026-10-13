using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

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

        public static double CalculateScore(int rating, int commentCount, DateTime creationDate, bool isClosed)
        {
            var order = Math.Log10(Math.Max(Math.Abs(rating), 1));

            var epoch = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var seconds = (creationDate.ToUniversalTime() - epoch).TotalSeconds;

            var commentBonus = Math.Log10(commentCount + 1) * 1.2;

            var closedBonus = isClosed ? -10 : 0;

            return order + seconds / 45000.0 + commentBonus + closedBonus;
        }
    }
}
