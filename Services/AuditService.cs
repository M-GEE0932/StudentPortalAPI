using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(int? userId, string action, string entityType, int? entityId, object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonDocument.Parse(JsonSerializer.Serialize(oldValues)) : null,
            NewValues = newValues != null ? JsonDocument.Parse(JsonSerializer.Serialize(newValues)) : null,
            Timestamp = DateTime.UtcNow,
            IPAddress = ipAddress
        };

        _auditRepository.Add(log);
        await _unitOfWork.SaveChangesAsync();

    }

    public async Task<List<AuditLog>> GetLogsAsync(int? userId = null, string? entityType = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _auditRepository.Query().Include(a => a.User).AsQueryable();

        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(entityType)) query = query.Where(a => a.EntityType == entityType);
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp <= to.Value);

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();

    }
}
