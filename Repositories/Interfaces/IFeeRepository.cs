using System.Linq.Expressions;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IFeeRepository : IGenericRepository<FeeRecord>
    {
        Task<IEnumerable<FeeRecord>> GetByStudentAsync(int studentId);
        Task<IEnumerable<FeeRecord>> GetAllWithStudentAsync();
        Task<(IEnumerable<FeeRecordDto> Items, int Total)> GetPagedDtosAsync(string? search, int page, int pageSize);
        Task<decimal> GetStudentBalanceAsync(int studentId);
        Task<bool> HasOverdueFeesAsync(int studentId);
        Task<FeeDashboardDto> GetDashboardDataAsync();

        //  Use Expression<Func<...>> for EF Core compatibility
        Task<decimal> SumAsync(Expression<Func<FeeRecord, decimal>> selector);
        Task<decimal> SumAsync(Expression<Func<FeeRecord, bool>> predicate, Expression<Func<FeeRecord, decimal>> selector);
    }
}