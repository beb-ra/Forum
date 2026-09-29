using ForumWebAPI.Data.Repositories;
using ForumWebAPI.Model;

namespace ForumWebAPI.Services
{
    public class BanService : IBanService
    {
        private readonly IHardDeleteRepository<Ban> _banRepository;

        public BanService(IHardDeleteRepository<Ban> banRepository)
        {
            _banRepository = banRepository;
        }
        public bool IsBanned(int userId, int? subforumId)
        {
            var now = DateTime.UtcNow;

            return _banRepository.Get().Any(b =>
                b.UserId == userId &&
                (b.ExpiresDate == null || b.ExpiresDate > now) &&
                (b.SubforumId == null || b.SubforumId == subforumId));
        }
    }
}
