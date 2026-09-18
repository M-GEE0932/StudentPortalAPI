using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class AuditRepository : GenericRepository<AuditLog>, IAuditRepository
    {
        public AuditRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AuditLog>> GetLogsAsync(int? userId = null, string? entityType = null, DateTime? from = null, DateTime? to = null)
        {
            var query = Query().Include(a => a.User).AsQueryable();

            if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
            if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
            if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

            return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
        }
    }
}
