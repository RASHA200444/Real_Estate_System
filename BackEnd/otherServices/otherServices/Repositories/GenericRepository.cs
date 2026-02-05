using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using System.Linq.Expressions;

namespace otherServices.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext2 _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext2 context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            foreach (var include in includes)
                query = query.Include(include);

            return await query.ToListAsync();
        }

        public IQueryable<T> GetAllQueryable()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<T> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }
        public async Task<T?> GetByIdAsync(long id, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            // Apply includes dynamically
            foreach (var include in includes)
                query = query.Include(include);

            // Detect the primary key dynamically
            var keyName = _context.Model.FindEntityType(typeof(T))?
                                .FindPrimaryKey()?.Properties
                                .Select(x => x.Name)
                                .Single();

            return await query.FirstOrDefaultAsync(e =>
                Microsoft.EntityFrameworkCore.EF.Property<long>(e, keyName) == id);

        }


        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public IQueryable<T> FindQueryable(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate).AsQueryable();
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }


        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            if (predicate == null)
                return await _context.Set<T>().CountAsync();

            return await _context.Set<T>().CountAsync(predicate);
        }

        public async Task<decimal> SumAsync( Expression<Func<T, bool>> predicate , Expression<Func<T, decimal>> selector )
        {
            if (predicate == null)
                return await _context.Set<T>().SumAsync(selector);

            return await _context.Set<T>().Where(predicate).SumAsync(selector);
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> NestedFind(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
            {
                foreach (var include in includes)
                    query = query.Include(include);
            }

            return await query.Where(predicate).ToListAsync();
        }
    }
}
