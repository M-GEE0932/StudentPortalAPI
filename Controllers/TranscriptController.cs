using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Services;
using StudentPortalAPI.Data;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TranscriptController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TranscriptController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetStudentTranscript(int studentId)
    {
        // Ownership: students may only view their own transcript.
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (role == "Student" && int.TryParse(userIdClaim, out var currentUserId))
        {
            var ownStudentId = await _context.Students
                .Where(s => s.UserId == currentUserId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
            if (ownStudentId != studentId)
                return Forbid();
        }

        var student = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null)
            return NotFound(new { message = "Student not found" });

        // Transcript displays every course with ANY graded component
        // (assignment / midterm / final / total), so a course whose assignment was
        // just graded appears immediately with no missing data. The OFFICIAL
        // cumulative CGPA below counts APPROVED courses only, matching
        // ResultRepository.GetStudentCGPAAsync (dashboard/profile source).
        var studentCourses = await _context.StudentCourses
            .Include(sc => sc.Course)
            .Include(sc => sc.AcademicYearSemester)
            .Where(sc => sc.StudentId == studentId &&
                (sc.TotalScore.HasValue || sc.AssignmentScore.HasValue ||
                 sc.MidtermScore.HasValue || sc.FinalScore.HasValue))
            .OrderBy(sc => sc.AcademicYearSemester!.YearNumber)
            .ThenBy(sc => sc.AcademicYearSemester!.SemesterNumber)
            .ToListAsync();

        // Official CGPA: approved courses only (authoritative rule).
        var approvedCourses = studentCourses.Where(sc => sc.IsApproved).ToList();
        var officialCredits = approvedCourses.Sum(sc => sc.Course?.Credits ?? 0);
        var officialGradePoints = approvedCourses.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0));
        var cumulativeCGPA = officialCredits > 0 ? Math.Round(officialGradePoints / officialCredits, 2) : 0m;

        var totalCredits = officialCredits;
        var totalGradePoints = officialGradePoints;

        var semesterData = studentCourses
            .GroupBy(sc => sc.AcademicYearSemesterId)
            .Select(g => new
            {
                SemesterId = g.Key,
                YearNumber = g.First().AcademicYearSemester?.YearNumber ?? 0,
                SemesterNumber = g.First().AcademicYearSemester?.SemesterNumber ?? 0,
                AcademicYear = g.First().AcademicYearSemester?.AcademicYear ?? "",
                SemesterCredits = g.Sum(sc => sc.Course?.Credits ?? 0),
                SemesterGradePoints = g.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0)),
                SemesterGPA = g.Sum(sc => sc.Course?.Credits ?? 0) > 0
                    ? Math.Round(g.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0)) / g.Sum(sc => sc.Course?.Credits ?? 0), 2)
                    : 0m,
                Courses = g.Select(sc => new
                {
                    sc.Course!.Code,
                    sc.Course.Title,
                    sc.Course.Credits,
                    sc.AssignmentScore,
                    sc.MidtermScore,
                    sc.FinalScore,
                    sc.TotalScore,
                    sc.Grade,
                    sc.GradePoints,
                    sc.IsApproved
                }).ToList()
            })
            .ToList();

        var response = new
        {
            studentInfo = new
            {
                student.Id,
                FullName = student.User?.FullName ?? "",
                StudentId = student.StudentId ?? "N/A",
                Email = student.User?.Email ?? "",
                DepartmentName = student.Department?.Name ?? "N/A",
                student.CurrentYear,
                student.CurrentSemester,
                student.EnrollmentDate,
                student.IsGraduated,
                CGPA = cumulativeCGPA,
                TotalCredits = totalCredits,
                TotalCourses = studentCourses.Count
            },
            cumulative = new
            {
                totalCredits,
                totalGradePoints = Math.Round(totalGradePoints, 2),
                cumulativeCGPA,
                totalCourses = studentCourses.Count
            },
            semesters = semesterData.Select(s => new
            {
                s.SemesterId,
                s.YearNumber,
                s.SemesterNumber,
                s.AcademicYear,
                SemesterLabel = $"Y{s.YearNumber}S{s.SemesterNumber}",
                s.SemesterCredits,
                s.SemesterGradePoints,
                s.SemesterGPA,
                CourseCount = s.Courses.Count,
                s.Courses
            }).ToList()
        };

        return Ok(response);
    }

    [HttpGet("pdf/{studentId}")]
    public async Task<IActionResult> GeneratePDFTranscript(int studentId)
    {
        return Ok(new { message = "PDF generation is handled client-side via jsPDF" });
    }
}
