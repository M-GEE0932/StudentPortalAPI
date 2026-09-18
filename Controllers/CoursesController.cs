using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<List<CourseDto>>> GetAll()
    {
        return Ok(await _courseService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id);
        if (course == null) return NotFound();
        return Ok(course);
    }

    [HttpGet("department/{deptId}")]
    public async Task<ActionResult<List<CourseDto>>> GetByDepartment(int deptId)
    {
        return Ok(await _courseService.GetByDepartmentAsync(deptId));
    }

    [HttpGet("faculty/{facultyId}")]
    public async Task<ActionResult<List<CourseDto>>> GetByFaculty(int facultyId)
    {
        return Ok(await _courseService.GetByFacultyAsync(facultyId));
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<CourseDto>>> GetByStudent(int studentId)
    {
        return Ok(await _courseService.GetByStudentAsync(studentId));
    }

    [HttpGet("year-semester/{ysId}")]
    public async Task<ActionResult<List<CourseDto>>> GetByYearSemester(int ysId)
    {
        return Ok(await _courseService.GetByYearSemesterAsync(ysId));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseRequest request)
    {
        var actorUserId = GetCurrentUserId();
        var result = await _courseService.CreateAsync(request, actorUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] UpdateCourseRequest request)
    {
        var result = await _courseService.UpdateAsync(id, request, GetCurrentUserId());
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _courseService.DeleteAsync(id, GetCurrentUserId());
        if (!deleted) return NotFound();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/assign-faculty")]
    public async Task<ActionResult> AssignFaculty(int id, [FromBody] AssignCourseFacultyRequest request)
    {
        var success = await _courseService.AssignFacultyAsync(id, request, GetCurrentUserId());
        if (!success) return NotFound();
        return Ok(new { message = "Faculty assigned successfully" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("enroll-student")]
    public async Task<ActionResult> EnrollStudent([FromQuery] int studentId, [FromQuery] int courseId, [FromQuery] int yearSemesterId)
    {
        var actorUserId = GetCurrentUserId();
        await _courseService.EnrollStudentAsync(studentId, courseId, yearSemesterId, actorUserId);
        return Ok(new { message = "Student enrolled successfully" });
    }
}
