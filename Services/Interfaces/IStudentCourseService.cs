namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IStudentCourseService
{
    Task<List<StudentCourseDto>> GetAssignedCoursesAsync(int studentId);
    Task<List<CourseDto>> GetAvailableCoursesAsync(int studentId);
    Task<StudentCourseDto?> AssignStudentToCourseAsync(AssignStudentCourseDto dto);
    Task<bool> RemoveEnrollmentAsync(int enrollmentId, int actorUserId);
    Task<List<StudentCourseDto>> BulkAssignAsync(BulkAssignDto dto);
}
