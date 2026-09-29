using Forum.Data;
using ForumWebAPI.Model;

namespace ForumWebAPI.Data.Repositories
{
    public class EFHardDeleteRepository<T> : EFRepository<T>, IHardDeleteRepository<T> where T : class
    {
        public EFHardDeleteRepository(ForumDbContext context) : base(context) { }
        public T? Delete(int id)
        {
            T? item = Get(id);
            if (item == null) return null;

            _context.Set<T>().Remove(item);
            _context.SaveChanges();
            return item;
        }
    }
}
