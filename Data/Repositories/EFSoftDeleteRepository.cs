using Forum.Data;
using ForumWebAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ForumWebAPI.Data.Repositories
{
    public class EFSoftDeleteRepository<T> : EFRepository<T>, ISoftDeleteRepository<T> where T : class, ISoftDelete
    {
        public EFSoftDeleteRepository(ForumDbContext context) : base(context) { }
        public T? Delete(int id)
        {
            T? item = Get(id);
            if (item == null) return null;

            item.IsDeleted = true;
            _context.SaveChanges();
            return item;
        }
    }
}
