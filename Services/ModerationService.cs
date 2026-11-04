using ForumWebAPI.Model;

namespace ForumWebAPI.Services
{
    public class ModerationService : IModerationService
    {
        public ModerationStatus CheckContent(string text, int subforumId, int userId)
        {
            // TODO: auto moderation
            return ModerationStatus.Pending;
        }
    }
}
