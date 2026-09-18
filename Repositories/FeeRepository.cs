using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class FeeRepository : GenericRepository<FeeRecord>, IFeeRepository
    {
        public FeeRepository(ApplicationDbContext context) : base(context) { }

        public new async Task<IEnumerable<FeeRecord>> GetAllAsync()
            => await Query()
                .Include(f => f.Student).ThenInclude(s => s!.User)
                .ToListAsync();

        public new async Task<FeeRecord?> GetByIdAsync(int id)
            => await Query()
                .Include(f => f.Student).ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(f => f.Id == id);

        public async Task<IEnumerable<FeeRecord>> GetByStudentAsync(int studentId)
            => await Query()
                .Where(f => f.StudentId == studentId)
                .Include(f => f.Student).ThenInclude(s => s!.User)
                .OrderByDescending(f => f.DueDate)
                .ToListAsync();

        public async Task<decimal> GetStudentBalanceAsync(int studentId)
        {
            var records = await Query()
                .Where(f => f.StudentId == studentId)
                .ToListAsync();

            return records
                .Where(f => f.Status != FeeStatus.Exempt && f.Status != FeeStatus.Paid)
                .Sum(f => f.Amount - f.AmountPaid);
        }

        public async Task<bool> HasOverdueFeesAsync(int studentId)
            => await Query().AnyAsync(f => f.StudentId == studentId && f.Status == FeeStatus.Overdue);

        public async Task<IEnumerable<FeeRecord>> GetAllWithStudentAsync()
            => await Query()
                .Include(f => f.Student).ThenInclude(s => s!.User)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

        public async Task<(IEnumerable<FeeRecordDto> Items, int Total)> GetPagedDtosAsync(string? search, int page, int pageSize)
        {
            var query = Query()
                .Include(f => f.Student).ThenInclude(s => s!.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Trim().ToLower().Split(' ');
                query = query.Where(f => f.Student != null && f.Student.User != null &&
                    terms.All(t => f.Student.User.FullName.ToLower().Contains(t)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeeRecordDto
                {
                    Id = f.Id,
                    StudentId = f.StudentId,
                    StudentName = f.Student != null && f.Student.User != null ? f.Student.User.FullName : null,
                    Amount = f.Amount,
                    AmountPaid = f.AmountPaid,
                    DueDate = f.DueDate,
                    PaidDate = f.PaidDate,
                    Status = f.Status.ToString(),
                    Description = f.Description,
                    TransactionId = f.TransactionId,
                    ExemptionType = f.ExemptionType
                })
                .ToListAsync();

            return (items, total);
        }

        public async Task<FeeDashboardDto> GetDashboardDataAsync()
        {
            var records = await Query().ToListAsync();

            return new FeeDashboardDto
            {
                TotalCollected = records.Where(f => f.Status == FeeStatus.Paid || f.Status == FeeStatus.PartiallyPaid).Sum(f => f.AmountPaid),
                TotalPending = records.Where(f => f.Status == FeeStatus.Pending).Sum(f => f.Amount - f.AmountPaid),
                TotalOverdue = records.Where(f => f.Status == FeeStatus.Overdue).Sum(f => f.Amount - f.AmountPaid),
                TotalStudents = await _context.Students.CountAsync(),
                PaidCount = records.Count(f => f.Status == FeeStatus.Paid),
                PendingCount = records.Count(f => f.Status == FeeStatus.Pending),
                OverdueCount = records.Count(f => f.Status == FeeStatus.Overdue),
                ExemptCount = records.Count(f => f.Status == FeeStatus.Exempt),
                PartiallyPaidCount = records.Count(f => f.Status == FeeStatus.PartiallyPaid)
            };
        }

        //  Correct: Use Expression<Func<...>> and call SumAsync directly on the queryable
        public async Task<decimal> SumAsync(Expression<Func<FeeRecord, decimal>> selector)
            => await Query().SumAsync(selector);

        public async Task<decimal> SumAsync(Expression<Func<FeeRecord, bool>> predicate, Expression<Func<FeeRecord, decimal>> selector)
            => await Query().Where(predicate).SumAsync(selector);
    }
}
