using ForumWebAPI.Model;

namespace ForumWebAPI.Data.Repositories
{
    public interface ISoftDeleteRepository<T> : IRepository<T> where T : class, ISoftDelete
    {
        T? Delete(int id);
    }
}
