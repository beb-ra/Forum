using ForumWebAPI.Model;

namespace ForumWebAPI.Services
{
    public interface IModerationService
    {
        ModerationStatus CheckContent(string text, int subforumId, int userId);
    }
}
