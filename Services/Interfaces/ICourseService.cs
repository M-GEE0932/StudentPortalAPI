namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface ICourseService
{
    Task<List<CourseDto>> GetAllAsync();
    Task<List<CourseDto>> GetByDepartmentAsync(int departmentId);
    Task<List<CourseDto>> GetByFacultyAsync(int facultyId);
    Task<List<CourseDto>> GetByStudentAsync(int studentId);
    Task<List<CourseDto>> GetByYearSemesterAsync(int yearSemesterId);
    Task<CourseDto?> GetByIdAsync(int id);
    Task<CourseDto> CreateAsync(CreateCourseRequest request, int actorUserId);
    Task<CourseDto?> UpdateAsync(int id, UpdateCourseRequest request, int actorUserId);
    Task<bool> DeleteAsync(int id, int actorUserId);
    Task<bool> AssignFacultyAsync(int courseId, AssignCourseFacultyRequest request, int actorUserId);
    Task<bool> EnrollStudentAsync(int studentId, int courseId, int yearSemesterId, int actorUserId);
}
