using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<StudentDto>>> GetAll()
    {
        return Ok(await _studentService.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await _studentService.GetByIdAsync(id);
        if (student == null) return NotFound();
        return Ok(student);
    }

    [Authorize]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<StudentDto>> GetByUserId(int userId)
    {
        var student = await _studentService.GetByUserIdAsync(userId);
        if (student == null) return NotFound();
        return Ok(student);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign")]
    public async Task<ActionResult<StudentDto>> AssignDepartment([FromBody] AssignStudentRequest request)
    {
        var result = await _studentService.AssignDepartmentAsync(request);
        return Ok(result);
    }

    [HttpGet("{id}/courses")]
    public async Task<ActionResult<List<CourseDto>>> GetEnrolledCourses(int id)
    {
        return Ok(await _studentService.GetEnrolledCoursesAsync(id));
    }

    [HttpGet("{id}/fee-status")]
    public async Task<ActionResult<bool>> CheckFeeStatus(int id)
    {
        return Ok(await _studentService.CheckFeeStatusAsync(id));
    }

    [HttpGet("{id}/attendance-check/{courseId}")]
    public async Task<ActionResult<bool>> CheckAttendance(int id, int courseId)
    {
        return Ok(await _studentService.CheckAttendanceGatekeeperAsync(id, courseId));
    }
}
