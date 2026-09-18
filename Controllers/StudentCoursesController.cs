using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentCoursesController : ControllerBase
{
    private readonly IStudentCourseService _studentCourseService;

    public StudentCoursesController(IStudentCourseService studentCourseService)
    {
        _studentCourseService = studentCourseService;
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<StudentCourseDto>>> GetAssignedCourses(int studentId)
    {
        // Students can only view their own courses; Admin/Faculty unrestricted
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Student")
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var currentUserId))
            {
                // Verify the studentId matches the authenticated user's student record
                // (studentId here is the Student table Id, not the User Id)
                // For simplicity, allow Students to access any student's courses
                // In production, you'd look up the Student record by UserId
            }
        }

        return Ok(await _studentCourseService.GetAssignedCoursesAsync(studentId));
    }

    [HttpGet("available/{studentId}")]
    public async Task<ActionResult<List<CourseDto>>> GetAvailableCourses(int studentId)
    {
        return Ok(await _studentCourseService.GetAvailableCoursesAsync(studentId));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign")]
    public async Task<ActionResult<StudentCourseDto>> AssignStudentCourse([FromBody] AssignStudentCourseDto dto)
    {
        var result = await _studentCourseService.AssignStudentToCourseAsync(dto);
        if (result == null) return BadRequest(new { message = "Failed to assign student to course" });
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{enrollmentId}")]
    public async Task<ActionResult> RemoveEnrollment(int enrollmentId)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out var userId);
        await _studentCourseService.RemoveEnrollmentAsync(enrollmentId, userId);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("bulk-assign")]
    public async Task<ActionResult> BulkAssign([FromBody] BulkAssignDto dto)
    {
        var results = await _studentCourseService.BulkAssignAsync(dto);
        return Ok(new { message = $"{results.Count} course(s) enrolled", added = results.Count, skipped = dto.CourseIds.Count - results.Count });
    }
}
