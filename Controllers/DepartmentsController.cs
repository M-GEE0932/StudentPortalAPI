using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DepartmentDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? durationYears)
    {
        return Ok(await _departmentService.GetAllAsync(search, durationYears));
    }

    /// <summary>
    /// Server-side paginated department list with per-department counts computed in SQL.
    /// </summary>
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<DepartmentDto>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int? durationYears,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _departmentService.GetPagedAsync(search, durationYears, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DepartmentDto>> GetById(int id)
    {
        var dept = await _departmentService.GetByIdAsync(id);
        if (dept == null) return NotFound();
        return Ok(dept);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<DepartmentDto>> Create([FromBody] CreateDepartmentRequest request)
    {
        var result = await _departmentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<DepartmentDto>> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _departmentService.UpdateAsync(id, request);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var deleted = await _departmentService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("{id}/year-semesters")]
    public async Task<ActionResult<List<AcademicYearSemesterDto>>> GetYearSemesters(int id)
    {
        return Ok(await _departmentService.GetYearSemestersAsync(id));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/generate-year-semesters")]
    public async Task<ActionResult<List<AcademicYearSemesterDto>>> GenerateYearSemesters(int id, [FromBody] CreateAcademicYearSemesterRequest request)
    {
        var result = await _departmentService.GenerateYearSemestersAsync(id, request.AcademicYear);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("year-semester/{id}")]
    public async Task<ActionResult> DeleteYearSemester(int id)
    {
        var deleted = await _departmentService.DeleteYearSemesterAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
