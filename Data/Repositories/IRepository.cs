using ForumWebAPI.Model;

namespace ForumWebAPI.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> Get();
        T? Get(int id);
        T Create(T item);
        void Update(int id, T item);
    }
}
