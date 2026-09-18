using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Get paginated, filterable notifications for the current user.
    /// GET /api/notifications?page=1&pageSize=20&read=false&type=info
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] NotificationFilter filter)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _notificationService.GetPagedAsync(userId, filter);
        return Ok(result);
    }

    /// <summary>
    /// Get the count of unread notifications for the current user.
    /// GET /api/notifications/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Get a single notification by ID (must belong to current user).
    /// GET /api/notifications/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var notif = await _notificationService.GetByIdAsync(id);
        if (notif == null || notif.UserId != CurrentUserId) return NotFound();
        return Ok(new NotificationDto
        {
            Id = notif.Id,
            UserId = notif.UserId,
            Title = notif.Title,
            Message = notif.Message,
            Type = notif.Type,
            CreatedAt = notif.CreatedAt,
            Read = notif.Read,
            Link = notif.Link
        });
    }

    /// <summary>
    /// Mark a single notification as read.
    /// PUT /api/notifications/{id}/mark-read
    /// </summary>
    [HttpPut("{id:guid}/mark-read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        if (!await OwnsNotification(id)) return NotFound();
        var success = await _notificationService.MarkAsReadAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Keep backward compatibility: PATCH /api/notifications/{id}/read
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsReadPatch(Guid id)
    {
        if (!await OwnsNotification(id)) return NotFound();
        var success = await _notificationService.MarkAsReadAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Mark a single notification as unread.
    /// PUT /api/notifications/{id}/mark-unread
    /// </summary>
    [HttpPut("{id:guid}/mark-unread")]
    public async Task<IActionResult> MarkAsUnread(Guid id)
    {
        if (!await OwnsNotification(id)) return NotFound();
        var success = await _notificationService.MarkUnreadAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Keep backward compatibility: PATCH /api/notifications/{id}/unread
    /// </summary>
    [HttpPatch("{id:guid}/unread")]
    public async Task<IActionResult> MarkAsUnreadPatch(Guid id)
    {
        if (!await OwnsNotification(id)) return NotFound();
        var success = await _notificationService.MarkUnreadAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read for the current user.
    /// PUT /api/notifications/mark-all-read
    /// </summary>
    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Keep backward compatibility: PATCH /api/notifications/read-all
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsReadPatch()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Soft-delete a notification (sets IsDeleted = true, hidden from queries but retained).
    /// DELETE /api/notifications/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _notificationService.SoftDeleteAsync(id, userId);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Hard-delete a notification (admin only, permanent removal).
    /// DELETE /api/notifications/{id}/permanent
    /// </summary>
    [HttpDelete("{id:guid}/permanent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PermanentDelete(Guid id)
    {
        var success = await _notificationService.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Create a notification for the authenticated user.
    /// POST /api/notifications
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var notification = new Notification
        {
            UserId = userId,
            Title = request.Title,
            Message = request.Message,
            Type = request.Type ?? "info",
            Link = request.Link,
            CreatedAt = DateTime.UtcNow,
            Read = false,
            IsDeleted = false
        };

        var created = await _notificationService.CreateAsync(notification);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new NotificationDto
        {
            Id = created.Id,
            UserId = created.UserId,
            Title = created.Title,
            Message = created.Message,
            Type = created.Type,
            CreatedAt = created.CreatedAt,
            Read = created.Read,
            Link = created.Link
        });
    }

    /// <summary>
    /// Ensures the notification exists and belongs to the authenticated user.
    /// </summary>
    private async Task<bool> OwnsNotification(Guid id)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrEmpty(userId)) return false;
        var notif = await _notificationService.GetByIdAsync(id);
        return notif != null && notif.UserId == userId;
    }
}
