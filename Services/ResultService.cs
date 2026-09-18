using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class ResultService : IResultService
{
    private readonly IResultRepository _resultRepository;
    private readonly IResultApprovalRepository _resultApprovalRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeeService _feeService;
    private readonly INotificationService _notificationService;

    public ResultService(
        IResultRepository resultRepository,
        IResultApprovalRepository resultApprovalRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IFeeService feeService,
        INotificationService notificationService)
    {
        _resultRepository = resultRepository;
        _resultApprovalRepository = resultApprovalRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _feeService = feeService;
        _notificationService = notificationService;
    }

    private static string CalculateGrade(decimal total)
    {
        if (total >= 90) return "A+";
        if (total >= 80) return "A";
        if (total >= 75) return "A-";
        if (total >= 70) return "B+";
        if (total >= 65) return "B";
        if (total >= 60) return "B-";
        if (total >= 55) return "C+";
        if (total >= 50) return "C";
        if (total >= 45) return "C-";
        if (total >= 40) return "D";
        return "F";
    }

    private static decimal CalculateGradePoints(string grade)
    {
        return grade switch
        {
            "A+" => 4.0m,
            "A" => 4.0m,
            "A-" => 3.75m,
            "B+" => 3.5m,
            "B" => 3.0m,
            "B-" => 2.75m,
            "C+" => 2.5m,
            "C" => 2.0m,
            "C-" => 1.75m,
            "D" => 1.0m,
            _ => 0.0m
        };
    }

    private async Task<List<ResultDto>> GetResultsByQuery(IQueryable<StudentCourse> query)
    {
        return await query.Select(sc => new ResultDto
        {
            Id = sc.Id,
            StudentCourseId = sc.Id,
            StudentId = sc.StudentId,
            StudentName = sc.Student != null && sc.Student.User != null ? sc.Student.User.FullName : null,
            CourseId = sc.CourseId,
            CourseName = sc.Course != null ? sc.Course.Title : null,
            CourseCode = sc.Course != null ? sc.Course.Code : null,
            MidtermScore = sc.MidtermScore,
            FinalScore = sc.FinalScore,
            AssignmentScore = sc.AssignmentScore,
            TotalScore = sc.TotalScore,
            Grade = sc.Grade,
            GradePoints = sc.GradePoints,
            IsApproved = sc.IsApproved,
            DepartmentName = sc.Course != null && sc.Course.Department != null ? sc.Course.Department.Name : null,
            YearNumber = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.YearNumber : null,
            SemesterNumber = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.SemesterNumber : null,
            AcademicYear = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.AcademicYear : null
        }).ToListAsync();
    }

    public async Task<List<ResultDto>> GetByStudentAsync(int studentId)
    {
        return await GetResultsByQuery(_resultRepository.Query()
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.AcademicYearSemester));
    }

    public async Task<List<ResultDto>> GetByCourseAsync(int courseId)
    {
        return await GetResultsByQuery(_resultRepository.Query()
            .Where(sc => sc.CourseId == courseId)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.AcademicYearSemester));
    }

    public async Task<List<ResultDto>> GetPendingApprovalsAsync()
    {
        return await GetResultsByQuery(_resultRepository.Query()
            .Where(sc => sc.TotalScore.HasValue && !sc.IsApproved)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.AcademicYearSemester));
    }

    public async Task<ResultDto?> GetByIdAsync(int studentCourseId)
    {
        var results = await GetResultsByQuery(_resultRepository.Query()
            .Where(sc => sc.Id == studentCourseId)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.AcademicYearSemester));

        return results.FirstOrDefault();
    }

    public async Task<bool> EnterResultAsync(EnterResultRequest request, int facultyUserId)
    {
        var sc = await _resultRepository.Query()
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course)
            .FirstOrDefaultAsync(sc => sc.Id == request.StudentCourseId)
            ?? throw new KeyNotFoundException("Student course enrollment not found");

        sc.AssignmentScore = Math.Min(25, Math.Max(0, request.AssignmentScore));
        sc.MidtermScore = Math.Min(25, Math.Max(0, request.MidtermScore));
        sc.FinalScore = Math.Min(50, Math.Max(0, request.FinalScore));
        sc.TotalScore = sc.AssignmentScore + sc.MidtermScore + sc.FinalScore;
        sc.Grade = CalculateGrade(sc.TotalScore.Value);
        sc.GradePoints = CalculateGradePoints(sc.Grade);
        sc.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Notify the student (PERSISTED)
        if (sc.Student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                sc.Student.UserId,
                "Result Entered",
                $"Your result for {sc.Course?.Title ?? "course"} has been entered. Total: {sc.TotalScore}, Grade: {sc.Grade}",
                "info",
                "/student/results");
        }

        // Notify the faculty (PERSISTED)
        await _notificationService.NotifyUserAsync(
            facultyUserId,
            "Result Entered",
            $"You entered a result for {sc.Student?.User?.FullName ?? "student"} in {sc.Course?.Title ?? "course"}.",
            "success");

        return true;
    }

    public async Task<bool> ApproveResultAsync(ApproveResultRequest request, int adminUserId)
    {
        var sc = await _resultRepository.Query()
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Course)
            .FirstOrDefaultAsync(sc => sc.Id == request.StudentCourseId)
            ?? throw new KeyNotFoundException("Student course enrollment not found");

        if (request.Status == "Approved")
        {
            var hasOverdue = await _feeService.HasOverdueFeesAsync(sc.StudentId);
            if (hasOverdue)
                throw new InvalidOperationException("Cannot approve results. Student has overdue fees.");
        }

        sc.IsApproved = request.Status == "Approved";
        sc.UpdatedAt = DateTime.UtcNow;

        if (sc.IsApproved)
        {
            await UpdateStudentCGPAAsync(sc.StudentId);
        }

        _resultApprovalRepository.Add(new ResultApproval
        {
            StudentCourseId = request.StudentCourseId,
            ApprovedByUserId = adminUserId,
            Status = request.Status == "Approved" ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
            Remarks = request.Remarks,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync();

        // Notify the student (PERSISTED)
        var statusMessage = request.Status == "Approved" ? "approved" : "rejected";
        var notificationType = request.Status == "Approved" ? "success" : "warning";
        if (sc.Student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                sc.Student.UserId,
                $"Result {statusMessage}",
                $"Your result for {sc.Course?.Title ?? "course"} has been {statusMessage}.",
                notificationType,
                "/student/results");
        }

        // Notify the admin (PERSISTED)
        await _notificationService.NotifyUserAsync(
            adminUserId,
            $"Result {statusMessage}",
            $"You {statusMessage} result for {sc.Student?.User?.FullName ?? "student"} in {sc.Course?.Title ?? "course"}.",
            "info");

        return true;
    }

    private async Task UpdateStudentCGPAAsync(int studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null) return;

        var approvedCourses = await _resultRepository.Query()
            .Where(sc => sc.StudentId == studentId && sc.IsApproved && sc.GradePoints.HasValue)
            .Include(sc => sc.Course)
            .ToListAsync();

        if (approvedCourses.Any())
        {
            var totalCredits = approvedCourses.Sum(sc => sc.Course?.Credits ?? 0);
            var weightedPoints = approvedCourses.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0));
            student.CGPA = totalCredits > 0 ? Math.Round(weightedPoints / totalCredits, 2) : 0;
            student.TotalCreditsEarned = totalCredits;
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<TranscriptDto?> GetTranscriptAsync(int studentId)
    {
        return await BuildTranscriptAsync(studentId, null);
    }

    public async Task<TranscriptDto?> GetTranscriptByYearSemesterAsync(int studentId, int? yearSemesterId)
    {
        return await BuildTranscriptAsync(studentId, yearSemesterId);
    }

    private async Task<TranscriptDto?> BuildTranscriptAsync(int studentId, int? yearSemesterId)
    {
        var student = await _studentRepository.Query()
            .Include(s => s.User)
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return null;

        var query = _resultRepository.Query()
            .Where(sc => sc.StudentId == studentId && sc.TotalScore.HasValue)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.AcademicYearSemester)
            .OrderBy(sc => sc.AcademicYearSemester!.YearNumber)
            .ThenBy(sc => sc.AcademicYearSemester!.SemesterNumber)
            .AsQueryable();

        if (yearSemesterId.HasValue)
            query = query.Where(sc => sc.AcademicYearSemesterId == yearSemesterId.Value);

        var entries = await query.Select(sc => new TranscriptEntryDto
        {
            CourseCode = sc.Course != null ? sc.Course.Code : "",
            CourseName = sc.Course != null ? sc.Course.Title : "",
            Credits = sc.Course != null ? sc.Course.Credits : 0,
            AssignmentScore = sc.AssignmentScore,
            MidtermScore = sc.MidtermScore,
            FinalScore = sc.FinalScore,
            TotalScore = sc.TotalScore,
            Grade = sc.Grade,
            GradePoints = sc.GradePoints,
            YearNumber = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.YearNumber : 0,
            SemesterNumber = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.SemesterNumber : 0,
            AcademicYear = sc.AcademicYearSemester != null ? sc.AcademicYearSemester.AcademicYear : ""
        }).ToListAsync();

        var totalCredits = entries.Sum(e => e.Credits);
        var weightedPoints = entries.Sum(e => (e.GradePoints ?? 0) * e.Credits);
        var calculatedCGPA = totalCredits > 0 ? Math.Round(weightedPoints / totalCredits, 2) : 0m;

        return new TranscriptDto
        {
            StudentName = student.User?.FullName ?? "",
            StudentIdNumber = student.StudentId,
            DepartmentName = student.Department?.Name,
            CGPA = calculatedCGPA,
            TotalCreditsEarned = totalCredits,
            Entries = entries
        };
    }

    public async Task<bool> CanPrintTranscriptAsync(int studentId)
    {
        return !await _feeService.HasOverdueFeesAsync(studentId);
    }
}
