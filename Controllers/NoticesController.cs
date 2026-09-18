using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NoticesController : ControllerBase
{
    private readonly INoticeService _noticeService;
    private readonly ApplicationDbContext _context;

    public NoticesController(INoticeService noticeService, ApplicationDbContext context)
    {
        _noticeService = noticeService;
        _context = context;
    }

    /// <summary>
    /// Get notices. Returns ALL notices for admins, user-relevant notices for others.
    /// Accepts optional ?page and ?pageSize for pagination.
    /// Without pagination params, returns a flat array (backward-compatible with Angular).
    /// GET /api/notices
    /// GET /api/notices?page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        // Admin: return all notices (flat array, backward-compatible)
        if (role == "Admin" && !page.HasValue)
        {
            return Ok(await _noticeService.GetAllAsync());
        }

        // Admin with pagination
        if (role == "Admin" && page.HasValue)
        {
            var allNotices = await _noticeService.GetAllAsync();
            var p = Math.Max(1, page.Value);
            var ps = Math.Clamp(pageSize ?? 20, 1, 50);
            var total = allNotices.Count;
            var items = allNotices.Skip((p - 1) * ps).Take(ps).ToList();
            return Ok(new PagedResult<NoticeDto>
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps
            });
        }

        // Non-admin: return user-relevant notices
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var deptIdClaim = User.FindFirst("DepartmentId");
        int? deptId = deptIdClaim != null ? int.Parse(deptIdClaim.Value) : null;

        if (!page.HasValue)
        {
            // Backward-compatible flat array
            return Ok(await _noticeService.GetForUserAsync(userId, role!, deptId));
        }

        // Non-admin with pagination
        var courseIds = await GetUserCourseIdsAsync(userId);
        var pagedResult = await _noticeService.GetNoticesForUserAsync(userId, role!, deptId, courseIds, page.Value, pageSize ?? 20);
        return Ok(pagedResult);
    }

    /// <summary>
    /// Get all notices (admin only, backward-compatible alias).
    /// GET /api/notices/all
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<NoticeDto>>> GetAll()
    {
        return Ok(await _noticeService.GetAllAsync());
    }

    /// <summary>
    /// Get notices for the current user (non-paginated, backward-compatible).
    /// GET /api/notices/my
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<NoticeDto>>> GetMyNotices()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var role = User.FindFirst(ClaimTypes.Role)!.Value;
        var deptIdClaim = User.FindFirst("DepartmentId");
        int? deptId = deptIdClaim != null ? int.Parse(deptIdClaim.Value) : null;
        return Ok(await _noticeService.GetForUserAsync(userId, role, deptId));
    }

    /// <summary>
    /// Get a single notice by ID (must be visible to the user).
    /// GET /api/notices/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var notice = await _noticeService.GetByIdAsync(id);
        if (notice == null) return NotFound();

        // Admin can see everything
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin") return Ok(notice);

        // Verify the user is allowed to see this notice
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var deptIdClaim = User.FindFirst("DepartmentId");
        int? deptId = deptIdClaim != null ? int.Parse(deptIdClaim.Value) : null;
        var courseIds = await GetUserCourseIdsAsync(userId);

        var targetType = Enum.Parse<NoticeTargetType>(notice.TargetType);
        bool isVisible = targetType switch
        {
            NoticeTargetType.All => true,
            NoticeTargetType.Department => notice.TargetDepartmentId == deptId,
            NoticeTargetType.Course => notice.TargetCourseId != null && courseIds.Contains(notice.TargetCourseId.Value),
            _ => false
        };

        if (!isVisible) return Forbid();
        return Ok(notice);
    }

    /// <summary>
    /// Create a new notice (admin or faculty).
    /// POST /api/notices
    /// </summary>
    [Authorize(Roles = "Admin,Faculty")]
    [HttpPost]
    public async Task<ActionResult<NoticeDto>> Create([FromBody] CreateNoticeRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _noticeService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Delete a notice (admin only).
    /// DELETE /api/notices/{id}
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        if (!await _noticeService.DeleteAsync(id)) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Toggle notice active/inactive status (admin only).
    /// PUT /api/notices/{id}/toggle-active
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/toggle-active")]
    public async Task<ActionResult> ToggleActive(int id)
    {
        if (!await _noticeService.ToggleActiveAsync(id)) return NotFound();
        return Ok(new { message = "Notice visibility toggled" });
    }

    // ═══════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Fetches the course IDs the user is enrolled in.
    /// </summary>
    private async Task<List<int>> GetUserCourseIdsAsync(int userId)
    {
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
        if (student == null) return new List<int>();

        return await _context.StudentCourses
            .Where(sc => sc.StudentId == student.Id)
            .Select(sc => sc.CourseId)
            .ToListAsync();
    }
}
