using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class ReportService : IReportService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IFeeRepository _feeRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAttendanceRepository _attendanceRepository;

    public ReportService(
        IStudentRepository studentRepository,
        IFacultyRepository facultyRepository,
        ICourseRepository courseRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IStudentCourseRepository studentCourseRepository,
        IFeeRepository feeRepository,
        IAssignmentRepository assignmentRepository,
        IAttendanceRepository attendanceRepository)
    {
        _studentRepository = studentRepository;
        _facultyRepository = facultyRepository;
        _courseRepository = courseRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _studentCourseRepository = studentCourseRepository;
        _feeRepository = feeRepository;
        _assignmentRepository = assignmentRepository;
        _attendanceRepository = attendanceRepository;
    }

    public async Task<DashboardStatsDto> GetAdminDashboardAsync()
    {
        var totalStudents = await _studentRepository.Query().CountAsync();
        var totalFaculty = await _facultyRepository.Query().CountAsync();
        var totalCourses = await _courseRepository.Query().CountAsync();
        var totalDepartments = await _departmentRepository.Query().CountAsync();
        var pendingApprovals = await _userRepository.Query().CountAsync(u => !u.IsActive);
        var pendingResults = await _studentCourseRepository.Query().CountAsync(sc => sc.TotalScore.HasValue && !sc.IsApproved);

        var totalFees = await _feeRepository.Query().SumAsync(f => f.Amount);
        var paidFees = await _feeRepository.Query().SumAsync(f => f.AmountPaid);
        var overdueFees = await _feeRepository.Query()
            .Where(f => f.Status == FeeStatus.Overdue)
            .SumAsync(f => f.Amount - f.AmountPaid);

        return new DashboardStatsDto
        {
            TotalStudents = totalStudents,
            TotalFaculty = totalFaculty,
            TotalDepartments = totalDepartments,
            TotalCourses = totalCourses,
            PendingApprovals = pendingApprovals,
            PendingResults = pendingResults,
            TotalFees = totalFees,
            CollectedFees = paidFees,
            OverdueFees = overdueFees,
            FeeCollectionRate = totalFees > 0 ? Math.Round(paidFees / totalFees * 100, 1) : 0
        };
    }

    public async Task<StudentDashboardDto?> GetStudentDashboardAsync(int studentUserId)
    {
        var student = await _studentRepository.Query()
            .Include(s => s.User)
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.UserId == studentUserId);

        if (student == null) return null;

        var enrolledCourses = await _studentCourseRepository.Query()
            .Where(sc => sc.StudentId == student.Id)
            .Include(sc => sc.Course)
            .ToListAsync();

        var approvedCourses = enrolledCourses.Where(ec => ec.IsApproved && ec.TotalScore.HasValue).ToList();
        var totalCredits = approvedCourses.Sum(ec => ec.Course?.Credits ?? 0);
        var totalGradePoints = approvedCourses.Sum(ec => (ec.GradePoints ?? 0) * (ec.Course?.Credits ?? 0));
        var cgpa = totalCredits > 0 ? Math.Round(totalGradePoints / totalCredits, 2) : 0m;

        var courseIds = enrolledCourses.Select(ec => ec.CourseId).ToList();

        var pendingAssignments = await _assignmentRepository.Query()
            .Where(a => courseIds.Contains(a.CourseId))
            .SelectMany(a => a.Submissions.Where(s => s.StudentId == student.Id).DefaultIfEmpty())
            .CountAsync(s => s != null && !s.Score.HasValue);

        decimal attendancePercentage = 100;
        if (enrolledCourses.Any())
        {
            var totalAttendance = 0m;
            foreach (var ec in enrolledCourses)
            {
                var total = await _attendanceRepository.Query().CountAsync(a => a.StudentId == student.Id && a.CourseId == ec.CourseId);
                if (total > 0)
                {
                    var present = await _attendanceRepository.Query().CountAsync(a =>
                        a.StudentId == student.Id && a.CourseId == ec.CourseId &&
                        (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.Excused));
                    totalAttendance += (decimal)present / total * 100;
                }
                else
                {
                    totalAttendance += 100;
                }
            }
            attendancePercentage = Math.Round(totalAttendance / enrolledCourses.Count, 1);
        }

        var feeRecords = await _feeRepository.Query()
            .Where(f => f.StudentId == student.Id)
            .ToListAsync();

        var feeBalance = feeRecords
            .Where(f => f.Status != FeeStatus.Exempt && f.Status != FeeStatus.Paid)
            .Sum(f => f.Amount - f.AmountPaid);

        var recentCourses = await _studentCourseRepository.Query()
            .Where(sc => sc.StudentId == student.Id)
            .Include(sc => sc.Course)
            .Include(sc => sc.AcademicYearSemester)
            .OrderByDescending(sc => sc.AcademicYearSemester!.YearNumber)
            .ThenByDescending(sc => sc.AcademicYearSemester!.SemesterNumber)
            .Take(5)
            .Select(sc => new CourseSummaryDto
            {
                CourseId = sc.CourseId,
                Code = sc.Course != null ? sc.Course.Code : "",
                Title = sc.Course != null ? sc.Course.Title : "",
                TotalScore = sc.TotalScore,
                Grade = sc.Grade
            }).ToListAsync();

        return new StudentDashboardDto
        {
            StudentName = student.User?.FullName ?? "",
            DepartmentName = student.Department?.Name,
            CGPA = cgpa,
            EnrolledCourses = enrolledCourses.Count,
            ApprovedCourses = approvedCourses.Count,
            TotalCredits = totalCredits,
            PendingAssignments = pendingAssignments,
            AttendancePercentage = attendancePercentage,
            FeeBalance = feeBalance,
            RecentCourses = recentCourses
        };
    }

    public async Task<FacultyDashboardDto?> GetFacultyDashboardAsync(int facultyUserId)
    {
        var faculty = await _facultyRepository.Query()
            .Include(f => f.User)
            .Include(f => f.Department)
            .FirstOrDefaultAsync(f => f.UserId == facultyUserId);

        if (faculty == null) return null;

        var courses = await _courseRepository.Query()
            .Where(c => c.FacultyId == faculty.Id)
            .ToListAsync();

        var courseIds = courses.Select(c => c.Id).ToList();

        var totalStudents = await _studentCourseRepository.Query()
            .Where(sc => courseIds.Contains(sc.CourseId))
            .CountAsync();

        var pendingSubmissions = await _assignmentRepository.Query()
            .Where(a => courseIds.Contains(a.CourseId))
            .SelectMany(a => a.Submissions.DefaultIfEmpty())
            .CountAsync(s => s != null && !s!.Score.HasValue);

        var pendingResults = await _studentCourseRepository.Query()
            .Where(sc => courseIds.Contains(sc.CourseId) && sc.TotalScore.HasValue && !sc.IsApproved)
            .CountAsync();

        return new FacultyDashboardDto
        {
            FacultyName = faculty.User?.FullName ?? "",
            DepartmentName = faculty.Department?.Name,
            TotalCourses = courses.Count,
            TotalStudents = totalStudents,
            PendingSubmissions = pendingSubmissions,
            PendingResults = pendingResults
        };
    }
}
