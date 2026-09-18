using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _courseRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public CourseService(
        ICourseRepository courseRepository,
        IFacultyRepository facultyRepository,
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _courseRepository = courseRepository;
        _facultyRepository = facultyRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    private IQueryable<CourseDto> GetCourseDtoQuery() => _courseRepository.Query()
        .Include(c => c.Department)
        .Include(c => c.AcademicYearSemester)
        .Include(c => c.Faculty).ThenInclude(f => f!.User)
        .Select(c => new CourseDto
        {
            Id = c.Id,
            Code = c.Code,
            Title = c.Title,
            Credits = c.Credits,
            DepartmentId = c.DepartmentId,
            DepartmentName = c.Department != null ? c.Department.Name : null,
            AcademicYearSemesterId = c.AcademicYearSemesterId,
            AcademicYear = c.AcademicYearSemester != null ? c.AcademicYearSemester.AcademicYear : null,
            YearNumber = c.AcademicYearSemester != null ? c.AcademicYearSemester.YearNumber : null,
            SemesterNumber = c.AcademicYearSemester != null ? c.AcademicYearSemester.SemesterNumber : null,
            FacultyId = c.FacultyId,
            FacultyName = c.Faculty != null && c.Faculty.User != null ? c.Faculty.User.FullName : null,
            Description = c.Description,
            EnrolledStudents = c.StudentCourses.Count,
            CreatedAt = c.CreatedAt
        });

    public async Task<List<CourseDto>> GetAllAsync()
        => await GetCourseDtoQuery().ToListAsync();

    public async Task<List<CourseDto>> GetByDepartmentAsync(int departmentId)
        => await GetCourseDtoQuery().Where(c => c.DepartmentId == departmentId).ToListAsync();

    public async Task<List<CourseDto>> GetByFacultyAsync(int facultyId)
        => await GetCourseDtoQuery().Where(c => c.FacultyId == facultyId).ToListAsync();

    public async Task<List<CourseDto>> GetByYearSemesterAsync(int yearSemesterId)
        => await GetCourseDtoQuery().Where(c => c.AcademicYearSemesterId == yearSemesterId).ToListAsync();

    public async Task<List<CourseDto>> GetByStudentAsync(int studentId)
    {
        return await _studentCourseRepository.Query().Where(sc => sc.StudentId == studentId)
            .Select(sc => sc.Course!)
            .Where(c => c != null)
            .Select(c => new CourseDto
            {
                Id = c!.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department != null ? c.Department.Name : null,
                AcademicYearSemesterId = c.AcademicYearSemesterId,
                FacultyId = c.FacultyId,
                FacultyName = c.Faculty != null && c.Faculty.User != null ? c.Faculty.User.FullName : null,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            }).ToListAsync();
    }

    public async Task<CourseDto?> GetByIdAsync(int id)
        => await GetCourseDtoQuery().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CourseDto> CreateAsync(CreateCourseRequest request, int actorUserId)
    {
        if (await _courseRepository.Query().AnyAsync(c => c.Code == request.Code))
            throw new InvalidOperationException("Course code already exists");

        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            Credits = request.Credits,
            DepartmentId = request.DepartmentId,
            AcademicYearSemesterId = request.AcademicYearSemesterId,
            FacultyId = request.FacultyId,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _courseRepository.Add(course);
        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogAsync(null, "Create", "Course", course.Id, newValues: new { course.Code, course.Title });

        // Notify actor (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Course Created",
            $"You created course '{course.Title}' ({course.Code}).",
            "success");

        // Notify faculty if assigned (PERSISTED)
        if (request.FacultyId.HasValue)
        {
            var faculty = await _facultyRepository.Query()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == request.FacultyId.Value);
            if (faculty?.UserId > 0)
            {
                await _notificationService.NotifyUserAsync(
                    faculty.UserId,
                    "Faculty Assigned",
                    $"You have been assigned as faculty for course '{course.Title}' ({course.Code}).",
                    "info");
            }
        }

        return (await GetByIdAsync(course.Id))!;
    }

    public async Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request, int actorUserId)
    {
        var course = await _courseRepository.Query()
            .Include(c => c.Faculty).ThenInclude(f => f!.User)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return null;

        var oldFacultyId = course.FacultyId;

        if (!string.IsNullOrEmpty(request.Code) && request.Code.ToUpper() != course.Code)
        {
            if (await _courseRepository.Query().AnyAsync(c => c.Code == request.Code.ToUpper() && c.Id != id))
                throw new InvalidOperationException($"Course with code '{request.Code}' already exists");
            course.Code = request.Code.ToUpper();
        }

        if (request.Title != null) course.Title = request.Title;
        if (request.Credits.HasValue) course.Credits = request.Credits.Value;
        if (request.DepartmentId.HasValue) course.DepartmentId = request.DepartmentId.Value;
        if (request.AcademicYearSemesterId.HasValue) course.AcademicYearSemesterId = request.AcademicYearSemesterId.Value;
        if (request.FacultyId.HasValue) course.FacultyId = request.FacultyId;
        if (request.Description != null) course.Description = request.Description;

        course.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        // Notify actor (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Course Updated",
            $"You updated course '{course.Title}' ({course.Code}).",
            "success");

        // If faculty changed, notify new faculty (PERSISTED)
        if (request.FacultyId.HasValue && request.FacultyId.Value != oldFacultyId)
        {
            var newFaculty = await _facultyRepository.Query()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == request.FacultyId.Value);
            if (newFaculty?.UserId > 0)
            {
                await _notificationService.NotifyUserAsync(
                    newFaculty.UserId,
                    "Faculty Assigned",
                    $"You have been assigned as faculty for course '{course.Title}' ({course.Code}).",
                    "info");
            }
        }

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        var course = await _courseRepository.GetByIdAsync(id);
        if (course == null) return false;

        var courseName = course.Title;
        _courseRepository.Remove(course);
        await _unitOfWork.SaveChangesAsync();

        // Notify actor (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Course Deleted",
            $"You deleted course '{courseName}'.",
            "warning");

        return true;
    }

    public async Task<bool> AssignFacultyAsync(int courseId, AssignCourseFacultyRequest request, int actorUserId)
    {
        var course = await _courseRepository.Query()
            .Include(c => c.Faculty).ThenInclude(f => f!.User)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return false;

        var faculty = await _facultyRepository.Query()
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == request.FacultyId);
        if (faculty == null) return false;

        course.FacultyId = request.FacultyId;
        course.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        // Notify the faculty (PERSISTED)
        if (faculty.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                faculty.UserId,
                "Faculty Assigned",
                $"You have been assigned as faculty for course '{course.Title}' ({course.Code}).",
                "info");
        }

        // Notify actor (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Faculty Assigned",
            $"You assigned {faculty.User?.FullName ?? "faculty"} to course '{course.Title}'.",
            "success");

        return true;
    }

    public async Task<bool> EnrollStudentAsync(int studentId, int courseId, int yearSemesterId, int actorUserId)
    {
        if (await _studentCourseRepository.Query().AnyAsync(sc =>
            sc.StudentId == studentId && sc.CourseId == courseId && sc.AcademicYearSemesterId == yearSemesterId))
            throw new InvalidOperationException("Student already enrolled in this course");

        var enrollment = new StudentCourse
        {
            StudentId = studentId,
            CourseId = courseId,
            AcademicYearSemesterId = yearSemesterId,
            EnrolledAt = DateTime.UtcNow,
            IsApproved = false
        };

        _studentCourseRepository.Add(enrollment);
        await _unitOfWork.SaveChangesAsync();

        var student = await _studentRepository.Query()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        var course = await _courseRepository.GetByIdAsync(courseId);

        // Notify the student (PERSISTED)
        if (student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                student.UserId,
                "Course Enrollment",
                $"You have been enrolled in course '{course?.Title}' ({course?.Code}).",
                "info",
                "/student/courses");
        }

        // Notify actor (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Student Enrolled",
            $"You enrolled {student?.User?.FullName ?? "student"} in course '{course?.Title}'.",
            "success");

        return true;
    }
}
