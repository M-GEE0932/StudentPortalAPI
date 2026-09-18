using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IAuditRepository : IGenericRepository<AuditLog>
    {
        Task<IEnumerable<AuditLog>> GetLogsAsync(int? userId = null, string? entityType = null, DateTime? from = null, DateTime? to = null);
    }
}