using System.Linq.Expressions;

namespace otherServices.Repositories
{
    public interface IGenericRepository <T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
        IQueryable<T> GetAllQueryable();

        Task<T> GetByIdAsync(long id);
        Task<T?> GetByIdAsync(long id, params Expression<Func<T, object>>[] includes);


        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        IQueryable<T> FindQueryable(Expression<Func<T, bool>> predicate);
        
        Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task<IEnumerable<T>> NestedFind(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);

        Task SaveChangesAsync();


    }
}
