using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
public CourseRepository(ApplicationDbContext context) : base(context) { }

        public override async Task<IEnumerable<Course>> GetAllAsync()
            => await _dbSet
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .ToListAsync();

        public async Task<Course?> GetCourseWithDetailsAsync(int courseId)
            => await _dbSet
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .FirstOrDefaultAsync(c => c.Id == courseId);

        public async Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(int departmentId)
            => await _dbSet
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .Where(c => c.DepartmentId == departmentId)
                .ToListAsync();

        public async Task<IEnumerable<Course>> GetCoursesByFacultyAsync(int facultyId)
            => await _dbSet
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .Where(c => c.FacultyId == facultyId)
                .ToListAsync();

        public async Task<IEnumerable<Course>> GetCoursesByStudentAsync(int studentId)
        {
            var courseIds = await _context.StudentCourses
                .Where(sc => sc.StudentId == studentId)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            return await _dbSet
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .Where(c => courseIds.Contains(c.Id))
                .ToListAsync();
        }
        public async Task<IEnumerable<Course>> GetCoursesByYearSemesterAsync(int yearSemesterId)
        {
            return await _dbSet
                .Where(c => c.AcademicYearSemesterId == yearSemesterId)
                .Include(c => c.Department)
                .Include(c => c.AcademicYearSemester)
                .Include(c => c.Faculty).ThenInclude(f => f!.User)
                .Include(c => c.StudentCourses)
                .ToListAsync();
        }
        public async Task<bool> CourseCodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _dbSet.Where(c => c.Code == code.ToUpper());
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}