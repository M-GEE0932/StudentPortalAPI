using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Data;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly IResultService _resultService;
    private readonly ApplicationDbContext _context;

    public ResultsController(IResultService resultService, ApplicationDbContext context)
    {
        _resultService = resultService;
        _context = context;
    }

    /// <summary>
    /// Ensures students can only read their own results; Admin/Faculty are unrestricted.
    /// </summary>
    private async Task<bool> StudentOwnsAsync(int studentId)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin" || role == "Faculty") return true;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(userIdClaim, out var currentUserId)) return false;

        return await _context.Students
            .AnyAsync(s => s.Id == studentId && s.UserId == currentUserId);
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<ResultDto>>> GetByStudent(int studentId)
    {
        if (!await StudentOwnsAsync(studentId)) return Forbid();
        return Ok(await _resultService.GetByStudentAsync(studentId));
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<List<ResultDto>>> GetByCourse(int courseId)
    {
        return Ok(await _resultService.GetByCourseAsync(courseId));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<ActionResult<List<ResultDto>>> GetPendingApprovals()
    {
        return Ok(await _resultService.GetPendingApprovalsAsync());
    }

    [HttpGet("{studentCourseId}")]
    public async Task<ActionResult<ResultDto>> GetById(int studentCourseId)
    {
        var result = await _resultService.GetByIdAsync(studentCourseId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Faculty")]
    [HttpGet("course/{courseId}/enrollments")]
    public async Task<ActionResult> GetCourseEnrollments(int courseId)
    {
        var results = await _resultService.GetByCourseAsync(courseId);
        return Ok(results);
    }

    [Authorize(Roles = "Faculty")]
    [HttpPost("enter")]
    public async Task<ActionResult> EnterResult([FromBody] EnterResultRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _resultService.EnterResultAsync(request, userId);
        return Ok(new { message = "Result entered successfully" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("approve")]
    public async Task<ActionResult> ApproveResult([FromBody] ApproveResultRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _resultService.ApproveResultAsync(request, userId);
        return Ok(new { message = $"Result {request.Status.ToLower()}" });
    }

    [HttpGet("transcript/{studentId}")]
    public async Task<ActionResult<TranscriptDto>> GetTranscript(int studentId, [FromQuery] int? yearSemesterId)
    {
        var transcript = yearSemesterId.HasValue
            ? await _resultService.GetTranscriptByYearSemesterAsync(studentId, yearSemesterId)
            : await _resultService.GetTranscriptAsync(studentId);
        if (transcript == null) return NotFound();
        return Ok(transcript);
    }

    [HttpGet("transcript/{studentId}/can-print")]
    public async Task<ActionResult<bool>> CanPrintTranscript(int studentId)
    {
        return Ok(await _resultService.CanPrintTranscriptAsync(studentId));
    }
}
