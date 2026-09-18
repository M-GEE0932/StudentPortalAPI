namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

public interface INotificationService
{
    // ── Existing methods (backward-compatible) ──────────────────────
    Task<List<Notification>> GetAllAsync();
    Task<List<Notification>> GetByUserAsync(string userId);
    Task<Notification?> GetByIdAsync(Guid id);
    Task<Notification> CreateAsync(Notification notification);
    /// <summary>
    /// Persists a notification for a user and pushes it over SignalR.
    /// Persistence guarantees history survives refresh/disconnects;
    /// SignalR is delivery-only. Safe to call when the user is offline.
    /// </summary>
    Task NotifyUserAsync(int userId, string title, string message, string type = "info", string? link = null);
    Task<bool> MarkAsReadAsync(Guid id);
    Task<bool> MarkUnreadAsync(Guid id);
    Task<bool> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteAsync(Guid id);

    // ── New production-ready methods ────────────────────────────────

    /// <summary>
    /// Paginated, filterable notification query for the current user.
    /// </summary>
    Task<NotificationPagedResult> GetPagedAsync(string userId, NotificationFilter filter);

    /// <summary>
    /// Returns the number of unread, non-deleted notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// Soft-delete: sets IsDeleted = true so the notification is hidden
    /// from queries but retained for audit/history.
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid id, string userId);

    /// <summary>
    /// Hard-delete: permanently removes the notification from the database.
    /// Admin only.
    /// </summary>
    Task<bool> HardDeleteAsync(Guid id);

    /// <summary>
    /// Delete notifications older than the specified date.
    /// Used by the background archival service.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
}
