using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IFeeRepository _feeRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StudentService(
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository,
        ICourseRepository courseRepository,
        IFeeRepository feeRepository,
        IAttendanceRepository attendanceRepository,
        IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
        _courseRepository = courseRepository;
        _feeRepository = feeRepository;
        _attendanceRepository = attendanceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<StudentDto>> GetAllAsync()
    {
        var students = await _studentRepository.Query()
            .Include(s => s.User)
            .Include(s => s.Department)
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        var cgpaData = await _studentCourseRepository.Query()
            .Include(sc => sc.Course)
            .Where(sc => studentIds.Contains(sc.StudentId) && sc.IsApproved && sc.TotalScore.HasValue)
            .GroupBy(sc => sc.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                TotalCredits = g.Sum(sc => sc.Course!.Credits),
                TotalGradePoints = g.Sum(sc => (sc.GradePoints ?? 0) * sc.Course!.Credits)
            })
            .ToDictionaryAsync(x => x.StudentId, x => new
            {
                x.TotalCredits,
                CGPA = x.TotalCredits > 0 ? Math.Round(x.TotalGradePoints / x.TotalCredits, 2) : 0m
            });

        return students.Select(s =>
        {
            var data = cgpaData.GetValueOrDefault(s.Id);
            return new StudentDto
            {
                Id = s.Id,
                UserId = s.UserId,
                FullName = s.User!.FullName,
                Email = s.User.Email,
                PhotoUrl = s.User.PhotoUrl,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department != null ? s.Department.Name : null,
                StudentIdNumber = s.StudentId,
                CurrentYear = s.CurrentYear,
                CurrentSemester = s.CurrentSemester,
                CGPA = data?.CGPA ?? 0m,
                TotalCreditsEarned = data?.TotalCredits ?? 0,
                EnrollmentDate = s.EnrollmentDate,
                IsGraduated = s.IsGraduated,
                IsActive = s.User.IsActive
            };
        }).OrderBy(s => s.FullName).ToList();
    }

    public async Task<StudentDto?> GetByIdAsync(int id)
    {
        var s = await _studentRepository.Query()
            .Include(st => st.User)
            .Include(st => st.Department)
            .FirstOrDefaultAsync(st => st.Id == id);

        if (s == null) return null;

        var cgpaData = await _studentCourseRepository.Query()
            .Include(sc => sc.Course)
            .Where(sc => sc.StudentId == s.Id && sc.IsApproved && sc.TotalScore.HasValue)
            .GroupBy(sc => sc.StudentId)
            .Select(g => new
            {
                TotalCredits = g.Sum(sc => sc.Course!.Credits),
                TotalGradePoints = g.Sum(sc => (sc.GradePoints ?? 0) * sc.Course!.Credits)
            })
            .FirstOrDefaultAsync();

        var totalCredits = cgpaData?.TotalCredits ?? 0;
        var cgpa = totalCredits > 0 && cgpaData != null ? Math.Round(cgpaData.TotalGradePoints / totalCredits, 2) : 0m;

        return new StudentDto
        {
            Id = s.Id,
            UserId = s.UserId,
            FullName = s.User!.FullName,
            Email = s.User.Email,
            PhotoUrl = s.User.PhotoUrl,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.Department?.Name,
            StudentIdNumber = s.StudentId,
            CurrentYear = s.CurrentYear,
            CurrentSemester = s.CurrentSemester,
            CGPA = cgpa,
            TotalCreditsEarned = totalCredits,
            EnrollmentDate = s.EnrollmentDate,
            IsGraduated = s.IsGraduated,
            IsActive = s.User.IsActive
        };
    }

    public async Task<StudentDto?> GetByUserIdAsync(int userId)
    {
        var s = await _studentRepository.Query()
            .Include(st => st.User)
            .Include(st => st.Department)
            .FirstOrDefaultAsync(st => st.UserId == userId);

        if (s == null) return null;

        var cgpaData = await _studentCourseRepository.Query()
            .Include(sc => sc.Course)
            .Where(sc => sc.StudentId == s.Id && sc.IsApproved && sc.TotalScore.HasValue)
            .GroupBy(sc => sc.StudentId)
            .Select(g => new
            {
                TotalCredits = g.Sum(sc => sc.Course!.Credits),
                TotalGradePoints = g.Sum(sc => (sc.GradePoints ?? 0) * sc.Course!.Credits)
            })
            .FirstOrDefaultAsync();

        var totalCredits = cgpaData?.TotalCredits ?? 0;
        var cgpa = totalCredits > 0 && cgpaData != null ? Math.Round(cgpaData.TotalGradePoints / totalCredits, 2) : 0m;

        return new StudentDto
        {
            Id = s.Id,
            UserId = s.UserId,
            FullName = s.User!.FullName,
            Email = s.User.Email,
            PhotoUrl = s.User.PhotoUrl,
            DepartmentId = s.DepartmentId,
            DepartmentName = s.Department?.Name,
            StudentIdNumber = s.StudentId,
            CurrentYear = s.CurrentYear,
            CurrentSemester = s.CurrentSemester,
            CGPA = cgpa,
            TotalCreditsEarned = totalCredits,
            EnrollmentDate = s.EnrollmentDate,
            IsGraduated = s.IsGraduated,
            IsActive = s.User.IsActive
        };
    }

    public async Task<StudentDto> AssignDepartmentAsync(AssignStudentRequest request)
    {
        // FIXED: Include the User entity, and handle request.IsActive
        var student = await _studentRepository.Query()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId)
            ?? throw new KeyNotFoundException("Student not found");

        student.DepartmentId = request.DepartmentId;
        student.CurrentYear = request.CurrentYear;
        student.CurrentSemester = request.CurrentSemester;

        // Apply the IsActive status if sent from the frontend
        if (request.IsActive.HasValue)
        {
            student.User!.IsActive = request.IsActive.Value;
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var courseId in request.CourseIds)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course != null)
            {
                var ysId = course.AcademicYearSemesterId;
                if (!await _studentCourseRepository.Query().AnyAsync(sc =>
                    sc.StudentId == student.Id && sc.CourseId == courseId && sc.AcademicYearSemesterId == ysId))
                {
                    _studentCourseRepository.Add(new StudentCourse
                    {
                        StudentId = student.Id,
                        CourseId = courseId,
                        AcademicYearSemesterId = ysId,
                        EnrolledAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return (await GetByIdAsync(student.Id))!;
    }

    public async Task<List<CourseDto>> GetEnrolledCoursesAsync(int studentId)
    {
        return await _studentCourseRepository.Query()
            .Where(sc => sc.StudentId == studentId)
            .Include(sc => sc.Course).ThenInclude(c => c!.Department)
            .Include(sc => sc.Course).ThenInclude(c => c!.Faculty).ThenInclude(f => f!.User)
            .Select(sc => new CourseDto
            {
                Id = sc.Course!.Id,
                Code = sc.Course.Code,
                Title = sc.Course.Title,
                Credits = sc.Course.Credits,
                DepartmentId = sc.Course.DepartmentId,
                DepartmentName = sc.Course.Department != null ? sc.Course.Department.Name : null,
                FacultyId = sc.Course.FacultyId,
                FacultyName = sc.Course.Faculty != null && sc.Course.Faculty.User != null
                    ? sc.Course.Faculty.User.FullName : null,
                AcademicYearSemesterId = sc.Course.AcademicYearSemesterId,
                CreatedAt = sc.Course.CreatedAt
            }).ToListAsync();
    }

    public async Task<bool> CheckFeeStatusAsync(int studentId)
    {
        return !await _feeRepository.Query().AnyAsync(f =>
            f.StudentId == studentId && f.Status == FeeStatus.Overdue);
    }

    public async Task<bool> CheckAttendanceGatekeeperAsync(int studentId, int courseId)
    {
        var percentage = await GetAttendancePercentageAsync(studentId, courseId);
        return percentage >= 75;
    }

    private async Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId)
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
