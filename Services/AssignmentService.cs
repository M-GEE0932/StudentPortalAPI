using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class AssignmentService : IAssignmentService
{
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAssignmentSubmissionRepository _assignmentSubmissionRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttendanceService _attendanceService;
    private readonly INotificationService _notificationService;

    public AssignmentService(
        IAssignmentRepository assignmentRepository,
        IAssignmentSubmissionRepository assignmentSubmissionRepository,
        IFacultyRepository facultyRepository,
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository,
        IUnitOfWork unitOfWork,
        IAttendanceService attendanceService,
        INotificationService notificationService)
    {
        _assignmentRepository = assignmentRepository;
        _assignmentSubmissionRepository = assignmentSubmissionRepository;
        _facultyRepository = facultyRepository;
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
        _unitOfWork = unitOfWork;
        _attendanceService = attendanceService;
        _notificationService = notificationService;
    }

    public async Task<List<AssignmentDto>> GetByCourseAsync(int courseId)
    {
        return await _assignmentRepository.Query()
            .Where(a => a.CourseId == courseId)
            .Include(a => a.Course)
            .Include(a => a.Faculty).ThenInclude(f => f!.User)
            .Include(a => a.Submissions)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                CourseId = a.CourseId,
                CourseName = a.Course!.Title,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty != null && a.Faculty.User != null ? a.Faculty.User.FullName : null,
                FileUrl = a.FileUrl,
                DueDate = a.DueDate,
                MaxScore = a.MaxScore,
                SubmittedCount = a.Submissions.Count,
                CreatedAt = a.CreatedAt
            }).ToListAsync();
    }

    public async Task<List<AssignmentDto>> GetForStudentAsync(int studentId)
    {
        var courseIds = await _studentCourseRepository.Query()
            .Where(sc => sc.StudentId == studentId)
            .Select(sc => sc.CourseId)
            .ToListAsync();

        return await _assignmentRepository.Query()
            .Where(a => courseIds.Contains(a.CourseId))
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                CourseId = a.CourseId,
                CourseName = a.Course!.Title,
                FileUrl = a.FileUrl,
                DueDate = a.DueDate,
                MaxScore = a.MaxScore,
                SubmittedCount = a.Submissions.Count,
                HasSubmitted = a.Submissions.Any(s => s.StudentId == studentId),
                CreatedAt = a.CreatedAt
            }).ToListAsync();
    }

    public async Task<AssignmentDto?> GetByIdAsync(int id)
    {
        return await _assignmentRepository.Query()
            .Where(a => a.Id == id)
            .Include(a => a.Course)
            .Include(a => a.Faculty).ThenInclude(f => f!.User)
            .Include(a => a.Submissions)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                CourseId = a.CourseId,
                CourseName = a.Course!.Title,
                FacultyId = a.FacultyId,
                FacultyName = a.Faculty != null && a.Faculty.User != null ? a.Faculty.User.FullName : null,
                FileUrl = a.FileUrl,
                DueDate = a.DueDate,
                MaxScore = a.MaxScore,
                SubmittedCount = a.Submissions.Count,
                CreatedAt = a.CreatedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<AssignmentDto> CreateAsync(CreateAssignmentRequest request, int facultyUserId)
    {
        var faculty = await _facultyRepository.Query().FirstOrDefaultAsync(f => f.UserId == facultyUserId)
            ?? throw new UnauthorizedAccessException("Faculty profile not found");

        var dueDateUtc = request.DueDate.Kind == DateTimeKind.Utc
            ? request.DueDate
            : DateTime.SpecifyKind(request.DueDate, DateTimeKind.Utc);

        var assignment = new Assignment
        {
            Title = request.Title,
            Description = request.Description,
            CourseId = request.CourseId,
            FacultyId = faculty.Id,
            FileUrl = request.FileUrl,
            DueDate = dueDateUtc,
            MaxScore = request.MaxScore,
            CreatedAt = DateTime.UtcNow
        };

        _assignmentRepository.Add(assignment);
        await _unitOfWork.SaveChangesAsync();

        // Notify all students enrolled in this course (PERSISTED)
        var students = await _studentCourseRepository.Query()
            .Where(sc => sc.CourseId == request.CourseId)
            .Include(sc => sc.Student)
            .Select(sc => sc.Student)
            .ToListAsync();

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        var courseName = course?.Title ?? "the course";

        foreach (var student in students.Where(s => s != null && s.UserId > 0))
        {
            await _notificationService.NotifyUserAsync(
                student!.UserId,
                "New Assignment",
                $"New assignment '{request.Title}' posted in {courseName}.",
                "info",
                "/student/assignments");
        }

        // Notify the faculty who created it
        await _notificationService.NotifyUserAsync(
            facultyUserId,
            "Assignment Created",
            $"You created assignment '{request.Title}' for {courseName}.",
            "success");

        return (await GetByIdAsync(assignment.Id))!;
    }

    public async Task<bool> SubmitAsync(int assignmentId, int studentUserId, SubmitAssignmentRequest request)
    {
        var student = await _studentRepository.Query()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == studentUserId)
            ?? throw new UnauthorizedAccessException("Student profile not found");

        var assignment = await _assignmentRepository.Query()
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new KeyNotFoundException("Assignment not found");

        var percentage = await _attendanceService.GetAttendancePercentageAsync(student.Id, assignment.CourseId);
        if (percentage < 75)
            throw new InvalidOperationException($"Insufficient attendance ({percentage:F1}%). You need at least 75% attendance to submit assignments.");

        if (await _assignmentSubmissionRepository.Query().AnyAsync(s =>
            s.AssignmentId == assignmentId && s.StudentId == student.Id))
            throw new InvalidOperationException("You have already submitted this assignment");

        var submission = new AssignmentSubmission
        {
            AssignmentId = assignmentId,
            StudentId = student.Id,
            FileUrl = request.FileUrl,
            Comments = request.Comments,
            SubmittedAt = DateTime.UtcNow
        };

        _assignmentSubmissionRepository.Add(submission);
        await _unitOfWork.SaveChangesAsync();

        // Notify the faculty (PERSISTED)
        var faculty = await _facultyRepository.Query()
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == assignment.FacultyId);
        if (faculty?.UserId > 0)
        {
            var studentName = student.User?.FullName ?? student.StudentId ?? "Unknown";
            await _notificationService.NotifyUserAsync(
                faculty.UserId,
                "Assignment Submitted",
                $"Student {studentName} submitted assignment '{assignment.Title}'.",
                "info");
        }

        // Notify the student (confirmation - PERSISTED)
        await _notificationService.NotifyUserAsync(
            studentUserId,
            "Submission Received",
            $"You submitted assignment '{assignment.Title}'.",
            "success",
            "/student/assignments");

        return true;
    }

    public async Task<bool> GradeAsync(int submissionId, GradeSubmissionRequest request)
    {
        var submission = await _assignmentSubmissionRepository.Query()
            .Include(s => s.Student).ThenInclude(st => st!.User)
            .Include(s => s.Assignment)
            .FirstOrDefaultAsync(s => s.Id == submissionId)
            ?? throw new KeyNotFoundException("Submission not found");

        submission.Score = request.Score;
        submission.GradedAt = DateTime.UtcNow;

        var assignment = await _assignmentRepository.GetByIdAsync(submission.AssignmentId);
        if (assignment != null)
        {
            var studentCourse = await _studentCourseRepository.Query()
                .FirstOrDefaultAsync(sc => sc.StudentId == submission.StudentId && sc.CourseId == assignment.CourseId);
            if (studentCourse != null)
            {
                studentCourse.AssignmentScore = request.Score;
                studentCourse.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify the student (PERSISTED)
        if (submission.Student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                submission.Student.UserId,
                "Assignment Graded",
                $"Your submission for '{submission.Assignment?.Title ?? "assignment"}' has been graded. Score: {request.Score}",
                "success",
                "/student/assignments");
        }

        return true;
    }

    public async Task<List<AssignmentSubmissionDto>> GetSubmissionsByAssignmentAsync(int assignmentId)
    {
        return await _assignmentSubmissionRepository.Query()
            .Where(s => s.AssignmentId == assignmentId)
            .Include(s => s.Student).ThenInclude(st => st!.User)
            .Include(s => s.Assignment)
            .Select(s => new AssignmentSubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment!.Title,
                StudentId = s.StudentId,
                StudentName = s.Student != null && s.Student.User != null ? s.Student.User.FullName : null,
                StudentIdNumber = s.Student != null ? s.Student.StudentId : null,
                StudentEmail = s.Student != null && s.Student.User != null ? s.Student.User.Email : null,
                FileUrl = s.FileUrl,
                Comments = s.Comments,
                Score = s.Score,
                SubmittedAt = s.SubmittedAt,
                GradedAt = s.GradedAt
            }).ToListAsync();
    }

    public async Task<AssignmentSubmissionDto?> GetSubmissionByIdAsync(int submissionId)
    {
        return await _assignmentSubmissionRepository.Query()
            .Where(s => s.Id == submissionId)
            .Include(s => s.Student).ThenInclude(st => st!.User)
            .Include(s => s.Assignment)
            .Select(s => new AssignmentSubmissionDto
            {
                Id = s.Id,
                AssignmentId = s.AssignmentId,
                AssignmentTitle = s.Assignment!.Title,
                StudentId = s.StudentId,
                StudentName = s.Student != null && s.Student.User != null ? s.Student.User.FullName : null,
                StudentIdNumber = s.Student != null ? s.Student.StudentId : null,
                StudentEmail = s.Student != null && s.Student.User != null ? s.Student.User.Email : null,
                FileUrl = s.FileUrl,
                Comments = s.Comments,
                Score = s.Score,
                SubmittedAt = s.SubmittedAt,
                GradedAt = s.GradedAt
            }).FirstOrDefaultAsync();
    }

    public async Task<bool> HasStudentSubmittedAsync(int assignmentId, int studentId)
    {
        return await _assignmentSubmissionRepository.Query().AnyAsync(s =>
            s.AssignmentId == assignmentId && s.StudentId == studentId);
    }

    public async Task<bool> CheckAttendanceForSubmissionAsync(int studentId, int courseId)
    {
        var percentage = await _attendanceService.GetAttendancePercentageAsync(studentId, courseId);
        return percentage >= 75;
    }
}
