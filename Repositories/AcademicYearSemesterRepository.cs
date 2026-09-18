using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class AcademicYearSemesterRepository : GenericRepository<AcademicYearSemester>, IAcademicYearSemesterRepository
    {
        public AcademicYearSemesterRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AcademicYearSemester>> GetByDepartmentAsync(int departmentId)
            => await Query()
                .Where(a => a.DepartmentId == departmentId)
                .OrderBy(a => a.YearNumber)
                .ThenBy(a => a.SemesterNumber)
                .ToListAsync();

        public async Task<AcademicYearSemester?> GetByYearAndSemesterAsync(int departmentId, int yearNumber, int semesterNumber)
            => await Query()
                .FirstOrDefaultAsync(a => a.DepartmentId == departmentId
                    && a.YearNumber == yearNumber
                    && a.SemesterNumber == semesterNumber);

        public async Task<bool> ExistsAsync(int departmentId, int yearNumber, int semesterNumber)
            => await Query()
                .AnyAsync(a => a.DepartmentId == departmentId
                    && a.YearNumber == yearNumber
                    && a.SemesterNumber == semesterNumber);
    }
}
