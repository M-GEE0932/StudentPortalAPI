namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IFacultyService
{
    Task<PagedResult<FacultyDto>> GetAllPagedAsync(int page, int pageSize, string? search = null);
    Task<List<FacultyDto>> GetAllAsync();
    Task<FacultyDto?> GetByIdAsync(int id);
    Task<FacultyDto?> GetByUserIdAsync(int userId);
    Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto dto);
    Task<FacultyDto?> UpdateFacultyAsync(int id, UpdateFacultyDto dto);
    Task<bool> DeleteFacultyAsync(int id);
    Task<FacultyDto> AssignDepartmentAsync(AssignFacultyRequest request);
    Task<List<StudentDto>> GetStudentsByCourseAsync(int courseId);
}
