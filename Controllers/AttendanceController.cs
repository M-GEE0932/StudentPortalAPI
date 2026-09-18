using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        return userId;
    }

    [HttpGet("course/{courseId}/date/{date}")]
    public async Task<ActionResult<List<AttendanceDto>>> GetByCourseAndDate(int courseId, DateTime date)
    {
        return Ok(await _attendanceService.GetByCourseAndDateAsync(courseId, date));
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<AttendanceDto>>> GetByStudent(int studentId)
    {
        return Ok(await _attendanceService.GetByStudentAsync(studentId));
    }

    [HttpGet("student/{studentId}/course/{courseId}")]
    public async Task<ActionResult<List<AttendanceDto>>> GetByStudentAndCourse(int studentId, int courseId)
    {
        return Ok(await _attendanceService.GetByStudentAndCourseAsync(studentId, courseId));
    }

    [HttpGet("summary/course/{courseId}")]
    public async Task<ActionResult<List<AttendanceSummaryDto>>> GetSummaryByCourse(int courseId)
    {
        return Ok(await _attendanceService.GetSummaryByCourseAsync(courseId));
    }

    [HttpGet("summary/student/{studentId}/course/{courseId}")]
    public async Task<ActionResult<AttendanceSummaryDto>> GetSummaryByStudentAndCourse(int studentId, int courseId)
    {
        var summary = await _attendanceService.GetSummaryByStudentAndCourseAsync(studentId, courseId);
        if (summary == null) return NotFound();
        return Ok(summary);
    }

    [Authorize(Roles = "Faculty")]
    [HttpPost("mark")]
    public async Task<ActionResult> MarkAttendance([FromBody] MarkAttendanceRequest request)
    {
        var userId = GetCurrentUserId();
        await _attendanceService.MarkAttendanceAsync(request, userId);
        return Ok(new { message = "Attendance marked successfully" });
    }

    [HttpGet("percentage/{studentId}/{courseId}")]
    public async Task<ActionResult<decimal>> GetPercentage(int studentId, int courseId)
    {
        return Ok(await _attendanceService.GetAttendancePercentageAsync(studentId, courseId));
    }
}
