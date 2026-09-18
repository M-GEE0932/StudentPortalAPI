using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IDepartmentRepository : IGenericRepository<Department>
    {
        Task<Department?> GetDepartmentWithDetailsAsync(int id);
        Task<IEnumerable<Department>> GetDepartmentsAsync(string? searchTerm = null, int? durationYears = null);
        Task<IEnumerable<DepartmentDto>> GetDepartmentDtosAsync(string? searchTerm = null, int? durationYears = null);
        Task<(IEnumerable<DepartmentDto> Items, int Total)> GetPagedDtosAsync(string? searchTerm, int? durationYears, int page, int pageSize);
        Task<bool> DepartmentNameExistsAsync(string name, int? excludeId = null);
    }
}