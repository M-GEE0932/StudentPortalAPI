using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IFacultyRepository : IGenericRepository<Faculty>
    {
        Task<Faculty?> GetFacultyWithUserAsync(int facultyId);
        Task<Faculty?> GetFacultyByUserIdAsync(int userId);
        Task<IEnumerable<Faculty>> GetFacultiesByDepartmentAsync(int departmentId);
        Task<(IEnumerable<FacultyDto> Items, int Total)> GetPagedDtosAsync(int page, int pageSize, string? search);
    }
}