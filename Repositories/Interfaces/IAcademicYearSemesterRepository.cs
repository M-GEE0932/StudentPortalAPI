using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IAcademicYearSemesterRepository : IGenericRepository<AcademicYearSemester>
    {
        Task<IEnumerable<AcademicYearSemester>> GetByDepartmentAsync(int departmentId);
        Task<AcademicYearSemester?> GetByYearAndSemesterAsync(int departmentId, int yearNumber, int semesterNumber);
        Task<bool> ExistsAsync(int departmentId, int yearNumber, int semesterNumber);
    }
}