using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext context) : base(context) { }

        // Override to include User and Department
        public new async Task<IEnumerable<Student>> GetAllAsync()
            => await _dbSet
                .Include(s => s.User)
                .Include(s => s.Department)
                .ToListAsync();

        public async Task<Student?> GetStudentWithUserAsync(int studentId)
            => await _dbSet.Include(s => s.User).Include(s => s.Department).FirstOrDefaultAsync(s => s.Id == studentId);

        public async Task<Student?> GetStudentWithCoursesAsync(int studentId)
            => await _dbSet
                .Include(s => s.User)
                .Include(s => s.Department)
                .Include(s => s.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                .FirstOrDefaultAsync(s => s.Id == studentId);

        public async Task<Student?> GetStudentByUserIdAsync(int userId)
            => await _dbSet.Include(s => s.User).Include(s => s.Department).FirstOrDefaultAsync(s => s.UserId == userId);

        public async Task<IEnumerable<Student>> GetStudentsByDepartmentAsync(int departmentId)
            => await _dbSet.Include(s => s.User).Include(s => s.Department).Where(s => s.DepartmentId == departmentId).ToListAsync();

        public async Task<IEnumerable<Student>> GetStudentsByCourseAsync(int courseId)
        {
            return await _dbSet
                .Include(s => s.User)
                .Include(s => s.Department)
                .Where(s => s.StudentCourses.Any(sc => sc.CourseId == courseId))
                .ToListAsync();
        }
    }
}