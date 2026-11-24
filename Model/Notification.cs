using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public enum NotificationType
    {
        CommentOnPost,
        ReplyToComment,
        BanReceived
    }
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? SenderId { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? CommentId { get; set; }
        public int? BanId { get; set; }
        public int? PostId { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public User User { get; set; } = null!;
    }
}
