using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubContext _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        INotificationHubContext hubContext,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════
    //  EXISTING METHODS (backward-compatible, now with IsDeleted)
    // ═══════════════════════════════════════════════════════════════

    public async Task<List<Notification>> GetAllAsync()
    {
        return await _notificationRepository.Query()
            .AsNoTracking()
            .Where(n => !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetByUserAsync(string userId)
    {
        return await _notificationRepository.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id)
    {
        return await _notificationRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        _notificationRepository.Add(notification);
        await _unitOfWork.SaveChangesAsync();
        return notification;
    }

    public async Task NotifyUserAsync(int userId, string title, string message, string type = "info", string? link = null)
    {
        if (userId <= 0) return;

        var notification = new Notification
        {
            UserId = userId.ToString(),
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            CreatedAt = DateTime.UtcNow,
            Read = false,
            IsDeleted = false
        };

        await CreateAsync(notification);

        // Push real-time via SignalR with retry
        try
        {
            var userIdStr = userId.ToString();
            var unreadCount = await GetUnreadCountAsync(userIdStr);

            // Event 1: The full notification object so the UI can append it to the list
            await _hubContext.SendToUserAsync(userIdStr, "ReceiveNotification", new
            {
                id = notification.Id.ToString(),
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                link = notification.Link,
                timestamp = notification.CreatedAt,
                read = false
            });

            // Event 2: Updated unread count so the badge updates instantly
            await _hubContext.SendToUserAsync(userIdStr, "UnreadCountUpdated", new
            {
                unreadCount
            });
        }
        catch (Exception ex)
        {
            // SignalR delivery failure should not block the main operation
            _logger.LogWarning(ex, "Failed to push notification {NotificationId} via SignalR to user {UserId}", notification.Id, userId);
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var notif = await _notificationRepository.Query().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        if (notif == null) return false;

        notif.Read = true;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkUnreadAsync(Guid id)
    {
        var notif = await _notificationRepository.Query().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        if (notif == null) return false;

        notif.Read = false;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        await _notificationRepository.ExecuteMarkAllReadAsync(userId);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await HardDeleteAsync(id);
    }

    public async Task<bool> HardDeleteAsync(Guid id)
    {
        var notif = await _notificationRepository.Query().FirstOrDefaultAsync(n => n.Id == id);
        if (notif == null) return false;

        _notificationRepository.Remove(notif);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    // ═══════════════════════════════════════════════════════════════
    //  NEW PRODUCTION-READY METHODS
    // ═══════════════════════════════════════════════════════════════

    public async Task<NotificationPagedResult> GetPagedAsync(string userId, NotificationFilter filter)
    {
        var query = _notificationRepository.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (filter.Read.HasValue)
            query = query.Where(n => n.Read == filter.Read.Value);

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(n => n.Type == filter.Type);

        if (filter.FromDate.HasValue)
            query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                CreatedAt = n.CreatedAt,
                Read = n.Read,
                Link = n.Link,
                NoticeId = n.NoticeId
            })
            .ToListAsync();

        return new NotificationPagedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _notificationRepository.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.Read && !n.IsDeleted);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, string userId)
    {
        var notif = await _notificationRepository.Query().FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        if (notif == null) return false;

        // If userId is provided, enforce ownership
        if (!string.IsNullOrEmpty(userId) && notif.UserId != userId)
            return false;

        notif.IsDeleted = true;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        // Only hard-delete notifications that have been soft-deleted
        // This prevents accidental permanent deletion of active notifications
        return await _notificationRepository.ExecuteDeleteOlderThanAsync(cutoff);
    }
}
