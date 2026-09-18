using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacultyController : ControllerBase
{
    private readonly IFacultyService _facultyService;

    public FacultyController(IFacultyService facultyService)
    {
        _facultyService = facultyService;
    }

    /// <summary>
    /// Server-side paginated faculty list.
    /// Accepts pageNumber or page (alias) plus pageSize; defaults 1 / 10.
    /// Example: GET /api/faculty?pageNumber=2&pageSize=10
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery(Name = "pageNumber")] int? pageNumberAlias = null)
    {
        var effectivePage = pageNumberAlias ?? page;
        var result = await _facultyService.GetAllPagedAsync(effectivePage, pageSize, search);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllList()
    {
        var result = await _facultyService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FacultyDto>> GetById(int id)
    {
        var faculty = await _facultyService.GetByIdAsync(id);
        if (faculty == null) return NotFound();
        return Ok(faculty);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<FacultyDto>> GetByUserId(int userId)
    {
        var faculty = await _facultyService.GetByUserIdAsync(userId);
        if (faculty == null) return NotFound();
        return Ok(faculty);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFacultyDto dto)
    {
        var result = await _facultyService.CreateFacultyAsync(dto);
        return Ok(new { success = true, message = "Faculty created successfully", faculty = result });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFacultyDto dto)
    {
        var result = await _facultyService.UpdateFacultyAsync(id, dto);
        if (result == null) return NotFound();
        return Ok(new { success = true, message = "Faculty updated successfully", faculty = result });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _facultyService.DeleteFacultyAsync(id)) return NotFound();
        return Ok(new { success = true, message = "Faculty deleted successfully" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("assign")]
    public async Task<IActionResult> AssignDepartment([FromBody] AssignFacultyRequest request)
    {
        var result = await _facultyService.AssignDepartmentAsync(request);
        return Ok(new { success = true, message = "Faculty updated", faculty = result });
    }

    [HttpGet("course/{courseId}/students")]
    public async Task<ActionResult<List<StudentDto>>> GetStudentsByCourse(int courseId)
    {
        return Ok(await _facultyService.GetStudentsByCourseAsync(courseId));
    }
}
