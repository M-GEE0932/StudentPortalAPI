using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Services;

public class NoticeService : INoticeService
{
    private readonly INoticeRepository _noticeRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubContext _hubContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<NoticeService> _logger;

    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;
    private const int MaxNotificationMessageLength = 200;

    public NoticeService(
        INoticeRepository noticeRepository,
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IFacultyRepository facultyRepository,
        IStudentCourseRepository studentCourseRepository,
        IUnitOfWork unitOfWork,
        INotificationHubContext hubContext,
        INotificationService notificationService,
        ILogger<NoticeService> logger)
    {
        _noticeRepository = noticeRepository;
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _facultyRepository = facultyRepository;
        _studentCourseRepository = studentCourseRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<List<NoticeDto>> GetAllAsync()
    {
        return await _noticeRepository.Query()
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoticeDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                PostedByUserId = n.PostedByUserId,
                PostedByName = n.PostedByUser != null ? n.PostedByUser.FullName : null,
                TargetType = n.TargetType.ToString(),
                TargetDepartmentId = n.TargetDepartmentId,
                TargetDepartmentName = n.TargetDepartment != null ? n.TargetDepartment.Name : null,
                TargetCourseId = n.TargetCourseId,
                TargetCourseName = n.TargetCourse != null ? n.TargetCourse.Title : null,
                CreatedAt = n.CreatedAt,
                ExpiryDate = n.ExpiryDate,
                IsActive = n.IsActive
            }).ToListAsync();
    }

    public async Task<List<NoticeDto>> GetForUserAsync(int userId, string role, int? departmentId)
    {
        var query = _noticeRepository.Query()
            .AsNoTracking()
            .Where(n => n.IsActive)
            .AsQueryable();

        query = query.Where(n =>
            n.TargetType == NoticeTargetType.All
            || (n.TargetType == NoticeTargetType.Department && n.TargetDepartmentId == departmentId)
            || n.TargetType == NoticeTargetType.Course);

        return await query.OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoticeDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                PostedByUserId = n.PostedByUserId,
                PostedByName = n.PostedByUser != null ? n.PostedByUser.FullName : null,
                TargetType = n.TargetType.ToString(),
                TargetDepartmentId = n.TargetDepartmentId,
                TargetDepartmentName = n.TargetDepartment != null ? n.TargetDepartment.Name : null,
                TargetCourseId = n.TargetCourseId,
                TargetCourseName = n.TargetCourse != null ? n.TargetCourse.Title : null,
                CreatedAt = n.CreatedAt,
                ExpiryDate = n.ExpiryDate,
                IsActive = n.IsActive
            }).ToListAsync();
    }

    public async Task<NoticeDto?> GetByIdAsync(int id)
    {
        return await _noticeRepository.Query()
            .AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new NoticeDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                PostedByUserId = n.PostedByUserId,
                PostedByName = n.PostedByUser != null ? n.PostedByUser.FullName : null,
                TargetType = n.TargetType.ToString(),
                TargetDepartmentId = n.TargetDepartmentId,
                TargetDepartmentName = n.TargetDepartment != null ? n.TargetDepartment.Name : null,
                TargetCourseId = n.TargetCourseId,
                TargetCourseName = n.TargetCourse != null ? n.TargetCourse.Title : null,
                CreatedAt = n.CreatedAt,
                ExpiryDate = n.ExpiryDate,
                IsActive = n.IsActive
            }).FirstOrDefaultAsync();
    }

    public async Task<NoticeDto> CreateAsync(CreateNoticeRequest request, int userId)
    {
        var targetType = Enum.Parse<NoticeTargetType>(request.TargetType);

        var notice = new Notice
        {
            Title = request.Title,
            Content = request.Content,
            PostedByUserId = userId,
            TargetType = targetType,
            TargetDepartmentId = request.TargetDepartmentId,
            TargetCourseId = request.TargetCourseId,
            TargetAcademicYearSemesterId = request.TargetAcademicYearSemesterId,
            ExpiryDate = request.ExpiryDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _noticeRepository.Add(notice);
        await _unitOfWork.SaveChangesAsync();

        var dto = await GetByIdAsync(notice.Id);

        // ═══════════════════════════════════════════════════════════
        // 1. Create personal notifications for each targeted user
        // ═══════════════════════════════════════════════════════════
        var targetUserIds = await GetTargetUserIdsAsync(targetType, request.TargetDepartmentId, request.TargetCourseId);

        // Exclude the creator to avoid self-notification
        targetUserIds.Remove(userId);

        if (targetUserIds.Count > 0)
        {
            var truncatedMessage = request.Content.Length > MaxNotificationMessageLength
                ? request.Content[..MaxNotificationMessageLength] + "..."
                : request.Content;

            var notifications = targetUserIds.Select(uid => new Notification
            {
                UserId = uid.ToString(),
                Title = $"New Notice: {notice.Title}",
                Message = truncatedMessage,
                Type = "notice",
                Link = $"/notices/{notice.Id}",
                CreatedAt = DateTime.UtcNow,
                Read = false,
                IsDeleted = false,
                NoticeId = notice.Id
            }).ToList();

            // Batch insert for efficiency
            _notificationRepository.AddRange(notifications);
            await _unitOfWork.SaveChangesAsync();

            // ═══════════════════════════════════════════════════════
            // 2. Send real-time SignalR to each user with the notification
            //    and updated unread count
            // ═══════════════════════════════════════════════════════
            foreach (var notification in notifications)
            {
                try
                {
                    var unreadCount = await _notificationService.GetUnreadCountAsync(notification.UserId);

                    // Send the notification object
                    await _hubContext.SendToUserAsync(notification.UserId, "ReceiveNotification", new
                    {
                        id = notification.Id.ToString(),
                        title = notification.Title,
                        message = notification.Message,
                        type = notification.Type,
                        link = notification.Link,
                        timestamp = notification.CreatedAt,
                        read = false,
                        noticeId = notification.NoticeId
                    });

                    // Send updated unread count for badge
                    await _hubContext.SendToUserAsync(notification.UserId, "UnreadCountUpdated", new
                    {
                        unreadCount
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send SignalR notification to user {UserId} for notice {NoticeId}",
                        notification.UserId, notice.Id);
                }
            }

            _logger.LogInformation("Created {Count} personal notifications for notice '{NoticeTitle}'",
                notifications.Count, notice.Title);
        }

        // ═══════════════════════════════════════════════════════════
        // 3. Broadcast to SignalR groups (for the notices page live update)
        // ═══════════════════════════════════════════════════════════
        await BroadcastNoticeAsync(dto!);

        return dto!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var notice = await _noticeRepository.GetByIdAsync(id);
        if (notice == null) return false;

        _noticeRepository.Remove(notice);
        await _unitOfWork.SaveChangesAsync();

        // Broadcast deletion to relevant groups
        var deletionPayload = new
        {
            noticeId = notice.Id,
            title = notice.Title,
            message = $"Notice '{notice.Title}' was deleted.",
            type = "warning",
            timestamp = DateTime.UtcNow
        };

        await BroadcastByTargetTypeAsync(notice.TargetType, notice.TargetDepartmentId,
            notice.TargetCourseId, "NoticeDeleted", deletionPayload);

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var notice = await _noticeRepository.GetByIdAsync(id);
        if (notice == null) return false;

        notice.IsActive = !notice.IsActive;
        await _unitOfWork.SaveChangesAsync();

        // Broadcast toggle to relevant groups
        var togglePayload = new
        {
            noticeId = notice.Id,
            title = notice.Title,
            message = $"Notice '{notice.Title}' visibility toggled.",
            type = "info",
            timestamp = DateTime.UtcNow
        };

        await BroadcastByTargetTypeAsync(notice.TargetType, notice.TargetDepartmentId,
            notice.TargetCourseId, "NoticeUpdated", togglePayload);

        return true;
    }

    public async Task<PagedResult<NoticeDto>> GetNoticesForUserAsync(
        int userId, string role, int? departmentId, IEnumerable<int> courseIds,
        int page, int pageSize)
    {
        var courseIdsList = courseIds?.ToList() ?? new List<int>();

        var query = _noticeRepository.Query()
            .AsNoTracking()
            .Where(n => n.IsActive)
            .Where(n => n.ExpiryDate == null || n.ExpiryDate > DateTime.UtcNow)
            .AsQueryable();

        // Targeted filtering: include notices relevant to this user
        query = query.Where(n =>
            n.TargetType == NoticeTargetType.All
            || (n.TargetType == NoticeTargetType.Department && n.TargetDepartmentId == departmentId)
            || (n.TargetType == NoticeTargetType.Course && n.TargetCourseId != null && courseIdsList.Contains(n.TargetCourseId.Value)));

        var totalCount = await query.CountAsync();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NoticeDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                PostedByUserId = n.PostedByUserId,
                PostedByName = n.PostedByUser != null ? n.PostedByUser.FullName : null,
                TargetType = n.TargetType.ToString(),
                TargetDepartmentId = n.TargetDepartmentId,
                TargetDepartmentName = n.TargetDepartment != null ? n.TargetDepartment.Name : null,
                TargetCourseId = n.TargetCourseId,
                TargetCourseName = n.TargetCourse != null ? n.TargetCourse.Title : null,
                CreatedAt = n.CreatedAt,
                ExpiryDate = n.ExpiryDate,
                IsActive = n.IsActive
            })
            .ToListAsync();

        return new PagedResult<NoticeDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves the list of User IDs who should receive a personal notification
    /// based on the notice target type.
    /// </summary>
    private async Task<List<int>> GetTargetUserIdsAsync(
        NoticeTargetType targetType, int? departmentId, int? courseId)
    {
        return targetType switch
        {
            NoticeTargetType.All => await _userRepository.Query()
                .Where(u => u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(),

            NoticeTargetType.Department when departmentId.HasValue =>
                await GetDepartmentUserIdsAsync(departmentId.Value),

            NoticeTargetType.Course when courseId.HasValue =>
                await GetCourseUserIdsAsync(courseId.Value),

            _ => new List<int>()
        };
    }

    /// <summary>
    /// Gets User IDs for all students and faculty in a department.
    /// </summary>
    private async Task<List<int>> GetDepartmentUserIdsAsync(int departmentId)
    {
        var studentUserIds = await _studentRepository.Query()
            .Where(s => s.DepartmentId == departmentId)
            .Select(s => s.UserId)
            .ToListAsync();

        var facultyUserIds = await _facultyRepository.Query()
            .Where(f => f.DepartmentId == departmentId)
            .Select(f => f.UserId)
            .ToListAsync();

        return studentUserIds.Concat(facultyUserIds).Distinct().ToList();
    }

    /// <summary>
    /// Gets User IDs for all students enrolled in a course.
    /// </summary>
    private async Task<List<int>> GetCourseUserIdsAsync(int courseId)
    {
        return await _studentCourseRepository.Query()
            .Where(sc => sc.CourseId == courseId)
            .Join(_studentRepository.Query(),
                sc => sc.StudentId,
                s => s.Id,
                (sc, s) => s.UserId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Broadcast a new notice to the appropriate SignalR groups.
    /// </summary>
    private async Task BroadcastNoticeAsync(NoticeDto notice)
    {
        var payload = new
        {
            id = notice.Id,
            title = notice.Title,
            content = notice.Content,
            targetType = notice.TargetType,
            postedByName = notice.PostedByName,
            createdAt = notice.CreatedAt
        };

        var targetType = Enum.Parse<NoticeTargetType>(notice.TargetType);
        await BroadcastByTargetTypeAsync(targetType, notice.TargetDepartmentId,
            notice.TargetCourseId, "NewNotice", payload);
    }

    /// <summary>
    /// Broadcast a SignalR message to groups based on the notice target type.
    /// </summary>
    private async Task BroadcastByTargetTypeAsync(
        NoticeTargetType targetType,
        int? departmentId,
        int? courseId,
        string method,
        object payload)
    {
        switch (targetType)
        {
            case NoticeTargetType.All:
                await SendToAllWithRetryAsync(method, payload);
                break;

            case NoticeTargetType.Department when departmentId.HasValue:
                await SendToGroupWithRetryAsync($"department_{departmentId.Value}", method, payload);
                break;

            case NoticeTargetType.Course when courseId.HasValue:
                await SendToGroupWithRetryAsync($"course_{courseId.Value}", method, payload);
                break;

            default:
                _logger.LogWarning("Notice has unexpected targeting: type={Type}, deptId={DeptId}, courseId={CourseId}",
                    targetType, departmentId, courseId);
                await SendToAllWithRetryAsync(method, payload);
                break;
        }
    }

    private async Task SendToGroupWithRetryAsync(string groupName, string method, object? arg1)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await _hubContext.SendToGroupAsync(groupName, method, arg1);
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

    private async Task SendToAllWithRetryAsync(string method, object? arg1)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await _hubContext.SendToAllAsync(method, arg1);
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
