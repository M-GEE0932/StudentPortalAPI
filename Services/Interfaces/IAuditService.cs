namespace StudentPortalAPI.Services;

using StudentPortalAPI.Models;

public interface IAuditService
{
    Task LogAsync(int? userId, string action, string entityType, int? entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null);
    Task<List<AuditLog>> GetLogsAsync(int? userId = null, string? entityType = null, DateTime? from = null, DateTime? to = null);
}
