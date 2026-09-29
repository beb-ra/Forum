namespace ForumWebAPI.Data.Repositories
{
    public interface IHardDeleteRepository<T> : IRepository<T> where T : class
    {
        T? Delete(int id);
    }
}
