using System.Linq;
using System.Linq.Expressions;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Exposes the underlying DbSet as IQueryable so services can compose
        /// LINQ queries (Include, Select projections, aggregations) without
        /// prematurely materializing data.
        /// </summary>
        IQueryable<T> Query();

        // Staged mutation methods (mirror DbSet semantics).
        // Changes are tracked but NOT saved; callers commit via IUnitOfWork.SaveChangesAsync().
        void Add(T entity);
        void AddRange(IEnumerable<T> entities);
        void Update(T entity);
        void Remove(T entity);

        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task DeleteAsync(T entity);
        Task SaveChangesAsync();
    }
}