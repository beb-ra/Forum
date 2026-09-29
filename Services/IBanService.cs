namespace ForumWebAPI.Services
{
    public interface IBanService
    {
        bool IsBanned(int userId, int? subforumId);
    }
}
