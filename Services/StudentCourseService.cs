namespace StudentPortalAPI.Services;

using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

public class StudentCourseService : IStudentCourseService
{
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly INotificationHubContext _hubContext;
    private readonly IUnitOfWork _unitOfWork;

    public StudentCourseService(
        IStudentCourseRepository studentCourseRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        INotificationHubContext hubContext,
        IUnitOfWork unitOfWork)
    {
        _studentCourseRepository = studentCourseRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _hubContext = hubContext;
        _unitOfWork = unitOfWork;
    }

    private static StudentCourseDto MapToDto(StudentCourse sc) => new()
    {
        Id = sc.Id,
        StudentId = sc.StudentId,
        StudentName = sc.Student?.User?.FullName,
        StudentIdNumber = sc.Student?.StudentId,
        CourseId = sc.CourseId,
        CourseCode = sc.Course?.Code,
        CourseTitle = sc.Course?.Title,
        Credits = sc.Course?.Credits ?? 0,
        DepartmentId = sc.Course?.DepartmentId ?? 0,
        DepartmentName = sc.Course?.Department?.Name,
        AcademicYearSemesterId = sc.AcademicYearSemesterId,
        AcademicYear = sc.Course?.AcademicYearSemester?.AcademicYear,
        YearNumber = sc.Course?.AcademicYearSemester?.YearNumber,
        SemesterNumber = sc.Course?.AcademicYearSemester?.SemesterNumber,
        FacultyName = sc.Course?.Faculty?.User?.FullName,
        MidtermScore = sc.MidtermScore,
        FinalScore = sc.FinalScore,
        TotalScore = sc.TotalScore,
        Grade = sc.Grade,
        GradePoints = sc.GradePoints,
        IsApproved = sc.IsApproved,
        EnrolledAt = sc.EnrolledAt
    };

    public async Task<List<StudentCourseDto>> GetAssignedCoursesAsync(int studentId)
    {
        var enrollments = await _studentCourseRepository.GetByStudentAsync(studentId);
        return enrollments.Select(MapToDto).ToList();
    }

    public async Task<List<CourseDto>> GetAvailableCoursesAsync(int studentId)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null) throw new KeyNotFoundException("Student not found");

        var enrolledCourseIds = (await _studentCourseRepository.GetByStudentAsync(studentId))
            .Select(sc => sc.CourseId).ToHashSet();

        var allCourses = await _courseRepository.GetAllAsync();
        var available = allCourses
            .Where(c => !enrolledCourseIds.Contains(c.Id)
                && c.DepartmentId == student.DepartmentId
                && c.AcademicYearSemester != null
                && c.AcademicYearSemester.YearNumber == student.CurrentYear
                && c.AcademicYearSemester.SemesterNumber == student.CurrentSemester)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Code = c.Code,
                Title = c.Title,
                Credits = c.Credits,
                DepartmentId = c.DepartmentId,
                DepartmentName = c.Department?.Name,
                AcademicYearSemesterId = c.AcademicYearSemesterId,
                AcademicYear = c.AcademicYearSemester?.AcademicYear,
                YearNumber = c.AcademicYearSemester?.YearNumber,
                SemesterNumber = c.AcademicYearSemester?.SemesterNumber,
                FacultyId = c.FacultyId,
                FacultyName = c.Faculty?.User?.FullName,
                Description = c.Description,
                EnrolledStudents = c.StudentCourses?.Count ?? 0,
                CreatedAt = c.CreatedAt
            }).ToList();

        return available;
    }

    public async Task<StudentCourseDto?> AssignStudentToCourseAsync(AssignStudentCourseDto dto)
    {
        var student = await _studentRepository.GetByIdAsync(dto.StudentId)
            ?? throw new KeyNotFoundException("Student not found");

        var course = await _courseRepository.GetByIdAsync(dto.CourseId)
            ?? throw new KeyNotFoundException("Course not found");

        if (await _studentCourseRepository.ExistsAsync(dto.StudentId, dto.CourseId))
            throw new InvalidOperationException("Student is already enrolled in this course");

        if (student.DepartmentId == null)
            throw new InvalidOperationException("Student must be assigned to a department before enrolling in courses");

        var enrollment = new StudentCourse
        {
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            AcademicYearSemesterId = course.AcademicYearSemesterId,
            EnrolledAt = DateTime.UtcNow,
            IsApproved = false
        };

        await _studentCourseRepository.AddAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        var result = await _studentCourseRepository.GetByIdWithDetailsAsync(enrollment.Id);
        return result != null ? MapToDto(result) : null;
    }

    public async Task<bool> RemoveEnrollmentAsync(int enrollmentId, int actorUserId)
    {
        var enrollment = await _studentCourseRepository.GetByIdAsync(enrollmentId)
            ?? throw new KeyNotFoundException("Enrollment not found");

        if (enrollment.IsApproved)
            throw new InvalidOperationException("Cannot remove an approved enrollment. Contact the result approver.");

        await _studentCourseRepository.DeleteAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<List<StudentCourseDto>> BulkAssignAsync(BulkAssignDto dto)
    {
        var student = await _studentRepository.GetByIdAsync(dto.StudentId)
            ?? throw new KeyNotFoundException("Student not found");

        if (student.DepartmentId == null)
            throw new InvalidOperationException("Student must be assigned to a department before enrolling in courses");

        var results = new List<StudentCourseDto>();

        foreach (var courseId in dto.CourseIds)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null) continue;

            if (await _studentCourseRepository.ExistsAsync(dto.StudentId, courseId))
                continue;

            var enrollment = new StudentCourse
            {
                StudentId = dto.StudentId,
                CourseId = courseId,
                AcademicYearSemesterId = course.AcademicYearSemesterId,
                EnrolledAt = DateTime.UtcNow,
                IsApproved = false
            };

            await _studentCourseRepository.AddAsync(enrollment);
            results.Add(new StudentCourseDto
            {
                StudentId = dto.StudentId,
                CourseId = courseId,
                CourseCode = course.Code,
                CourseTitle = course.Title,
                Credits = course.Credits,
                EnrolledAt = enrollment.EnrolledAt
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return results;
    }
}
