using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IFacultyService _facultyService;

    public AssignmentsController(IAssignmentService assignmentService, IFacultyService facultyService)
    {
        _assignmentService = assignmentService;
        _facultyService = facultyService;
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<List<AssignmentDto>>> GetByCourse(int courseId)
    {
        return Ok(await _assignmentService.GetByCourseAsync(courseId));
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<AssignmentDto>>> GetForStudent(int studentId)
    {
        return Ok(await _assignmentService.GetForStudentAsync(studentId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssignmentDto>> GetById(int id)
    {
        var assignment = await _assignmentService.GetByIdAsync(id);
        if (assignment == null) return NotFound();
        return Ok(assignment);
    }

    [Authorize(Roles = "Faculty")]
    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create([FromBody] CreateAssignmentRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _assignmentService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Student")]
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> Submit(int id, [FromBody] SubmitAssignmentRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _assignmentService.SubmitAsync(id, userId, request);
        return Ok(new { message = "Assignment submitted" });
    }

    [Authorize(Roles = "Faculty")]
    [HttpPost("submission/{submissionId}/grade")]
    public async Task<ActionResult> GradeSubmission(int submissionId, [FromBody] GradeSubmissionRequest request)
    {
        await _assignmentService.GradeAsync(submissionId, request);
        return Ok(new { message = "Graded successfully" });
    }

    [Authorize(Roles = "Student")]
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "File type not allowed. Allowed: PDF, DOC, DOCX, TXT, ZIP, RAR, JPG, PNG" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10MB limit" });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"/uploads/{fileName}";
        return Ok(new { fileUrl, fileName = file.FileName });
    }

    [Authorize(Roles = "Faculty")]
    [HttpGet("{id}/submissions")]
    public async Task<ActionResult<List<AssignmentSubmissionDto>>> GetSubmissions(int id)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var assignment = await _assignmentService.GetByIdAsync(id);
        if (assignment == null) return NotFound(new { message = "Assignment not found" });
        if (assignment.FacultyId == null) return Forbid("Assignment has no faculty assigned");
        var faculty = await _facultyService.GetByUserIdAsync(userId);
        if (faculty == null || faculty.Id != assignment.FacultyId) return Forbid("You can only view submissions for your own assignments");
        var submissions = await _assignmentService.GetSubmissionsByAssignmentAsync(id);
        return Ok(submissions);
    }

    [Authorize(Roles = "Faculty")]
    [HttpGet("submission/{submissionId}/download")]
    public async Task<ActionResult> DownloadSubmission(int submissionId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var submission = await _assignmentService.GetSubmissionByIdAsync(submissionId);
        if (submission == null) return NotFound(new { message = "Submission not found" });
        var assignment = await _assignmentService.GetByIdAsync(submission.AssignmentId);
        if (assignment == null) return NotFound(new { message = "Assignment not found" });
        var faculty = await _facultyService.GetByUserIdAsync(userId);
        if (faculty == null || faculty.Id != assignment.FacultyId) return Forbid("You can only download submissions for your own assignments");
        if (string.IsNullOrEmpty(submission.FileUrl)) return BadRequest(new { message = "No file attached" });
        return Ok(new { fileUrl = submission.FileUrl, fileName = Path.GetFileName(submission.FileUrl) });
    }
}
