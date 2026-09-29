using System.Text.Json.Serialization;

namespace ForumWebAPI.Model
{
    public class Ban
    {
        public int BanId { get; set; }
        public int UserId { get; set; }
        public int? SubforumId { get; set; }
        public int? BannedBy { get; set; }
        public string? BanReason { get; set; }
        public DateTime BannedDate { get; set; }
        public DateTime? ExpiresDate { get; set; }

        [JsonIgnore]
        public User User { get; set; } = null!;
        [JsonIgnore]
        public Subforum? Subforum { get; set; }
        [JsonIgnore]
        public User? BannedByUser { get; set; }
    }
}
