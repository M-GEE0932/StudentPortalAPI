using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class StudentCourseRepository : GenericRepository<StudentCourse>, IStudentCourseRepository
    {
        public StudentCourseRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<StudentCourse>> GetByStudentAsync(int studentId)
            => await _dbSet
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.Course).ThenInclude(c => c!.AcademicYearSemester)
                .Include(sc => sc.Course).ThenInclude(c => c!.Faculty).ThenInclude(f => f!.User)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();

        public async Task<IEnumerable<StudentCourse>> GetByCourseAsync(int courseId)
            => await _dbSet
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.Course).ThenInclude(c => c!.AcademicYearSemester)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();

        public async Task<StudentCourse?> GetByStudentAndCourseAsync(int studentId, int courseId)
            => await _dbSet.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        public async Task<bool> IsEnrolledAsync(int studentId, int courseId)
            => await _dbSet.AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        public async Task<bool> ExistsAsync(int studentId, int courseId)
            => await _dbSet.AnyAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

        public async Task<StudentCourse?> GetByIdWithDetailsAsync(int enrollmentId)
            => await _dbSet
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.Course).ThenInclude(c => c!.AcademicYearSemester)
                .Include(sc => sc.Course).ThenInclude(c => c!.Faculty).ThenInclude(f => f!.User)
                .Include(sc => sc.AcademicYearSemester)
                .FirstOrDefaultAsync(sc => sc.Id == enrollmentId);

        public async Task<IEnumerable<StudentCourse>> GetEnrolledWithDetailsAsync(int studentId)
            => await _dbSet
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.Course).ThenInclude(c => c!.Faculty).ThenInclude(f => f!.User)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();
    }
}