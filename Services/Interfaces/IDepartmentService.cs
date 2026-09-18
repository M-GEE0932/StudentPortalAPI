namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(string? searchTerm = null, int? durationYears = null);
    Task<PagedResult<DepartmentDto>> GetPagedAsync(string? searchTerm = null, int? durationYears = null, int page = 1, int pageSize = 10);
    Task<bool> DeleteYearSemesterAsync(int id);
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request);
    Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentRequest request);
    Task<bool> DeleteAsync(int id);
    Task<List<AcademicYearSemesterDto>> GetYearSemestersAsync(int departmentId);
    Task<List<AcademicYearSemesterDto>> GenerateYearSemestersAsync(int departmentId, string academicYear);
}
