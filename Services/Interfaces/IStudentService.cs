namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IStudentService
{
    Task<List<StudentDto>> GetAllAsync();
    Task<StudentDto?> GetByIdAsync(int id);
    Task<StudentDto?> GetByUserIdAsync(int userId);
    Task<StudentDto> AssignDepartmentAsync(AssignStudentRequest request);
    Task<List<CourseDto>> GetEnrolledCoursesAsync(int studentId);
    Task<bool> CheckFeeStatusAsync(int studentId);
    Task<bool> CheckAttendanceGatekeeperAsync(int studentId, int courseId);
}
