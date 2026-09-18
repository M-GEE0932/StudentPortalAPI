using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace StudentPortalAPI.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _context;

    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;

    public NotificationHub(
        INotificationService notificationService,
        ILogger<NotificationHub> logger,
        ApplicationDbContext context)
    {
        _notificationService = notificationService;
        _logger = logger;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Personal group: user_{userId}
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Role group
            if (!string.IsNullOrEmpty(role))
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{role}");

            // Join department groups based on user's profile
            await JoinDepartmentGroupsAsync(userId);

            // Join course groups based on enrolled courses
            await JoinCourseGroupsAsync(userId);

            _logger.LogInformation("User {UserId} (role={Role}) connected to NotificationHub with connection {ConnectionId}",
                userId, role, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Explicitly remove from all groups to avoid stale memberships
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            if (!string.IsNullOrEmpty(role))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role_{role}");

            // Remove from department and course groups
            await RemoveFromDepartmentGroupsAsync(userId);
            await RemoveFromCourseGroupsAsync(userId);
        }

        if (exception != null)
            _logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
        else
            _logger.LogInformation("User {UserId} disconnected normally", userId);

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows a client to explicitly join a department group (e.g., after profile update).
    /// </summary>
    public async Task JoinDepartmentGroup(int departmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"department_{departmentId}");
    }

    /// <summary>
    /// Allows a client to explicitly join a course group (e.g., after enrollment).
    /// </summary>
    public async Task JoinCourseGroup(int courseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"course_{courseId}");
    }

    /// <summary>
    /// Persist + send to specific user with retry.
    /// </summary>
    public async Task SendNotificationToUser(string userId, string message, string type = "info", string? link = null)
    {
        var notif = new Notification
        {
            UserId = userId,
            Title = "Notification",
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            Link = link
        };

        var created = await _notificationService.CreateAsync(notif);
        await SendWithRetryAsync($"user_{userId}", "ReceiveNotification", created);
    }

    /// <summary>
    /// Persist + send to role group with retry.
    /// </summary>
    public async Task SendNotificationToRole(string role, string message, string type = "info", string? link = null)
    {
        var notif = new Notification
        {
            UserId = role,
            Title = "Notification",
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            Link = link
        };

        var created = await _notificationService.CreateAsync(notif);
        await SendWithRetryAsync($"role_{role}", "ReceiveNotification", created);
    }

    /// <summary>
    /// Persist + send globally (admin only) with retry.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public async Task SendGlobalNotification(string message, string type = "info", string? link = null)
    {
        var notif = new Notification
        {
            UserId = "Global",
            Title = "Notification",
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow,
            Link = link
        };

        var created = await _notificationService.CreateAsync(notif);
        await SendToAllWithRetryAsync("ReceiveNotification", created);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    private async Task JoinDepartmentGroupsAsync(string userId)
    {
        try
        {
            if (int.TryParse(userId, out var userIdInt))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt);
                if (student?.DepartmentId != null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"department_{student.DepartmentId}");

                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.UserId == userIdInt);
                if (faculty?.DepartmentId != null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"department_{faculty.DepartmentId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to join department groups for user {UserId}", userId);
        }
    }

    private async Task JoinCourseGroupsAsync(string userId)
    {
        try
        {
            if (int.TryParse(userId, out var userIdInt))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt);
                if (student != null)
                {
                    var courseIds = await _context.StudentCourses
                        .Where(sc => sc.StudentId == student.Id)
                        .Select(sc => sc.CourseId)
                        .ToListAsync();

                    foreach (var courseId in courseIds)
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"course_{courseId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to join course groups for user {UserId}", userId);
        }
    }

    private async Task RemoveFromDepartmentGroupsAsync(string userId)
    {
        try
        {
            if (int.TryParse(userId, out var userIdInt))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt);
                if (student?.DepartmentId != null)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department_{student.DepartmentId}");

                var faculty = await _context.Faculties
                    .FirstOrDefaultAsync(f => f.UserId == userIdInt);
                if (faculty?.DepartmentId != null)
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department_{faculty.DepartmentId}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove from department groups for user {UserId}", userId);
        }
    }

    private async Task RemoveFromCourseGroupsAsync(string userId)
    {
        try
        {
            if (int.TryParse(userId, out var userIdInt))
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt);
                if (student != null)
                {
                    var courseIds = await _context.StudentCourses
                        .Where(sc => sc.StudentId == student.Id)
                        .Select(sc => sc.CourseId)
                        .ToListAsync();

                    foreach (var courseId in courseIds)
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"course_{courseId}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove from course groups for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Send to a specific group with retry logic.
    /// </summary>
    private async Task SendWithRetryAsync(string groupName, string method, object? arg1)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await Clients.Group(groupName).SendAsync(method, arg1);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR send to group {Group} failed (attempt {Attempt}/{MaxRetries})",
                    groupName, attempt, MaxRetries);

                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelayMs * attempt);
            }
        }
    }

    /// <summary>
    /// Send to all clients with retry logic.
    /// </summary>
    private async Task SendToAllWithRetryAsync(string method, object? arg1)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await Clients.All.SendAsync(method, arg1);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR broadcast failed (attempt {Attempt}/{MaxRetries})",
                    attempt, MaxRetries);

                if (attempt < MaxRetries)
                    await Task.Delay(RetryDelayMs * attempt);
            }
        }
    }
}
