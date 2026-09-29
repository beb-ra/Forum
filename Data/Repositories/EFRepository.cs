using Forum.Data;
using ForumWebAPI.Model;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace ForumWebAPI.Data.Repositories
{
    public class EFRepository<T> : IRepository<T> where T : class
    {
        protected readonly ForumDbContext _context;
        public EFRepository(ForumDbContext context)
        {
            _context = context;
        }
        public T Create(T item)
        {
            _context.Set<T>().Add(item);
            _context.SaveChanges();

            return item;
        }

        public IEnumerable<T> Get()
        {
            return _context.Set<T>().ToList();
        }

        public T? Get(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public void Update(int id, T updated)
        {
            T? current = _context.Set<T>().Find(id);
            if (current == null) return;

            _context.Entry(current).CurrentValues.SetValues(updated);
            _context.SaveChanges();
        }
    }
}
