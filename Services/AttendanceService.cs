using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IFacultyRepository facultyRepository,
        ICourseRepository courseRepository,
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _attendanceRepository = attendanceRepository;
        _facultyRepository = facultyRepository;
        _courseRepository = courseRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<List<AttendanceDto>> GetByCourseAndDateAsync(int courseId, DateTime date)
    {
        return await _attendanceRepository.Query()
            .Where(a => a.CourseId == courseId && a.Date.Date == date.Date)
            .Include(a => a.Student).ThenInclude(s => s!.User)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student!.User!.FullName,
                CourseId = a.CourseId,
                Date = a.Date,
                Status = a.Status.ToString(),
                Remarks = a.Remarks
            }).ToListAsync();
    }

    public async Task<List<AttendanceDto>> GetByStudentAsync(int studentId)
    {
        return await _attendanceRepository.Query()
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Course)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                CourseId = a.CourseId,
                CourseName = a.Course!.Title,
                Date = a.Date,
                Status = a.Status.ToString(),
                Remarks = a.Remarks
            }).ToListAsync();
    }

    public async Task<List<AttendanceDto>> GetByStudentAndCourseAsync(int studentId, int courseId)
    {
        return await _attendanceRepository.Query()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .OrderByDescending(a => a.Date)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.Student!.User!.FullName,
                CourseId = a.CourseId,
                Date = a.Date,
                Status = a.Status.ToString(),
                Remarks = a.Remarks
            }).ToListAsync();
    }

    public async Task<List<AttendanceSummaryDto>> GetSummaryByCourseAsync(int courseId)
    {
        var students = await _studentCourseRepository.Query()
            .Where(sc => sc.CourseId == courseId)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Select(sc => new { sc.StudentId, sc.Student!.User!.FullName })
            .ToListAsync();

        var result = new List<AttendanceSummaryDto>();

        foreach (var s in students)
        {
            var summary = await GetSummaryByStudentAndCourseAsync(s.StudentId, courseId);
            if (summary != null) result.Add(summary);
        }

        return result;
    }

    public async Task<AttendanceSummaryDto?> GetSummaryByStudentAndCourseAsync(int studentId, int courseId)
    {
        var records = await _attendanceRepository.Query()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .ToListAsync();

        if (!records.Any()) return null;

        var total = records.Count;
        var student = await _studentRepository.Query().Include(s => s.User).FirstOrDefaultAsync(s => s.Id == studentId);
        var course = await _courseRepository.GetByIdAsync(courseId);

        return new AttendanceSummaryDto
        {
            StudentId = studentId,
            StudentName = student?.User?.FullName,
            CourseId = courseId,
            CourseName = course?.Title,
            TotalClasses = total,
            PresentCount = records.Count(a => a.Status == AttendanceStatus.Present),
            AbsentCount = records.Count(a => a.Status == AttendanceStatus.Absent),
            LateCount = records.Count(a => a.Status == AttendanceStatus.Late),
            ExcusedCount = records.Count(a => a.Status == AttendanceStatus.Excused),
            AttendancePercentage = total > 0
                ? (decimal)(records.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.Excused)) / total * 100
                : 0
        };
    }

    public async Task<bool> MarkAttendanceAsync(MarkAttendanceRequest request, int facultyUserId)
    {
        var faculty = await _facultyRepository.Query().FirstOrDefaultAsync(f => f.UserId == facultyUserId)
            ?? throw new UnauthorizedAccessException("Faculty profile not found");

        var attendanceDate = request.Date.Kind == DateTimeKind.Utc
            ? request.Date.Date
            : DateTime.SpecifyKind(request.Date.Date, DateTimeKind.Utc);

        var course = await _courseRepository.GetByIdAsync(request.CourseId);
        var courseName = course?.Title ?? "the course";

        var studentIds = request.Entries.Select(e => e.StudentId).Distinct().ToList();
        var students = await _studentRepository.Query()
            .Where(s => studentIds.Contains(s.Id))
            .Include(s => s.User)
            .ToDictionaryAsync(s => s.Id);

        foreach (var entry in request.Entries)
        {
            var existing = await _attendanceRepository.Query().FirstOrDefaultAsync(a =>
                a.StudentId == entry.StudentId && a.CourseId == request.CourseId && a.Date.Date == attendanceDate);

            if (existing == null)
            {
                _attendanceRepository.Add(new Attendance
                {
                    StudentId = entry.StudentId,
                    CourseId = request.CourseId,
                    FacultyId = faculty.Id,
                    Date = attendanceDate,
                    Status = Enum.Parse<AttendanceStatus>(entry.Status, ignoreCase: true),
                    Remarks = entry.Remarks,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Status = Enum.Parse<AttendanceStatus>(entry.Status, ignoreCase: true);
                existing.Remarks = entry.Remarks;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify each student (PERSISTED)
        foreach (var entry in request.Entries)
        {
            if (students.TryGetValue(entry.StudentId, out var student) && student.UserId > 0)
            {
                if (student.User?.Role == UserRole.Student)
                {
                    await _notificationService.NotifyUserAsync(
                        student.UserId,
                        "Attendance Marked",
                        $"Attendance marked for {courseName} on {attendanceDate:yyyy-MM-dd}: {entry.Status}",
                        "info",
                        "/student/attendance");
                }
            }
        }

        // Notify the faculty (PERSISTED)
        await _notificationService.NotifyUserAsync(
            facultyUserId,
            "Attendance Recorded",
            $"You marked attendance for {request.Entries.Count} student(s) in {courseName} on {attendanceDate:yyyy-MM-dd}.",
            "success");

        return true;
    }

    public async Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId)
    {
        var total = await _attendanceRepository.Query().CountAsync(a =>
            a.StudentId == studentId && a.CourseId == courseId);

        if (total == 0) return 100;

        var present = await _attendanceRepository.Query().CountAsync(a =>
            a.StudentId == studentId && a.CourseId == courseId &&
            (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.Excused));

        return (decimal)present / total * 100;
    }
}
