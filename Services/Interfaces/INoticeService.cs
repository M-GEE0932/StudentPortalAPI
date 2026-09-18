namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface INoticeService
{
    // ── Existing methods (backward-compatible) ──────────────────────
    Task<List<NoticeDto>> GetAllAsync();
    Task<List<NoticeDto>> GetForUserAsync(int userId, string role, int? departmentId);
    Task<NoticeDto?> GetByIdAsync(int id);
    Task<NoticeDto> CreateAsync(CreateNoticeRequest request, int userId);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);

    // ── New production-ready methods ────────────────────────────────

    /// <summary>
    /// Returns paginated notices relevant to a user based on their role,
    /// department, and enrolled courses.
    /// Includes: TargetType.All always, Department if matching, Course if enrolled.
    /// Filters: IsActive == true and not expired.
    /// </summary>
    Task<PagedResult<NoticeDto>> GetNoticesForUserAsync(
        int userId,
        string role,
        int? departmentId,
        IEnumerable<int> courseIds,
        int page,
        int pageSize);
}
