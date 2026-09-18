using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StudentPortalAPI.Repositories
{
    public class ResultRepository : GenericRepository<StudentCourse>, IResultRepository
    {
        public ResultRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<StudentCourse>> GetResultsByStudentAsync(int studentId)
            => await Query()
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();

        public async Task<IEnumerable<StudentCourse>> GetResultsByCourseAsync(int courseId)
            => await Query()
                .Where(sc => sc.CourseId == courseId)
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();

        public async Task<IEnumerable<StudentCourse>> GetPendingApprovalsAsync()
            => await Query()
                .Where(sc => sc.TotalScore.HasValue && !sc.IsApproved)
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.AcademicYearSemester)
                .ToListAsync();

        public async Task<StudentCourse?> GetStudentCourseWithDetailsAsync(int studentCourseId)
            => await Query()
                .Where(sc => sc.Id == studentCourseId)
                .Include(sc => sc.Student).ThenInclude(s => s!.User)
                .Include(sc => sc.Course).ThenInclude(c => c!.Department)
                .Include(sc => sc.AcademicYearSemester)
                .FirstOrDefaultAsync();

        public async Task<decimal> GetStudentCGPAAsync(int studentId)
        {
            var approvedCourses = await Query()
                .Where(sc => sc.StudentId == studentId && sc.IsApproved && sc.GradePoints.HasValue)
                .Include(sc => sc.Course)
                .ToListAsync();

            if (!approvedCourses.Any()) return 0;

            var totalCredits = approvedCourses.Sum(sc => sc.Course?.Credits ?? 0);
            var weightedPoints = approvedCourses.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0));

            return totalCredits > 0 ? Math.Round(weightedPoints / totalCredits, 2) : 0;
        }

        public async Task UpdateStudentCGPAAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return;

            var cgpa = await GetStudentCGPAAsync(studentId);
            student.CGPA = cgpa;

            var totalCredits = await Query()
                .Where(sc => sc.StudentId == studentId && sc.IsApproved && sc.GradePoints.HasValue)
                .SumAsync(sc => sc.Course != null ? sc.Course.Credits : 0);

            student.TotalCreditsEarned = totalCredits;
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<int, decimal>> GetStudentCGPAListAsync(IEnumerable<int> studentIds)
        {
            var studentIdList = studentIds.ToList();
            if (!studentIdList.Any())
                return new Dictionary<int, decimal>();

            var query = from sc in Query()
                        where studentIdList.Contains(sc.StudentId)
                              && sc.IsApproved
                              && sc.GradePoints.HasValue
                        join c in _context.Courses on sc.CourseId equals c.Id into courseJoin
                        from c in courseJoin.DefaultIfEmpty()
                        group new { sc, c } by sc.StudentId into g
                        select new
                        {
                            StudentId = g.Key,
                            TotalCredits = g.Sum(x => x.c == null ? 0 : x.c.Credits),
                            WeightedPoints = g.Sum(x => (x.sc.GradePoints ?? 0) * (x.c == null ? 0 : x.c.Credits))
                        };

            var result = await query.ToListAsync();

            return result.ToDictionary(
                x => x.StudentId,
                x => x.TotalCredits > 0 ? Math.Round(x.WeightedPoints / x.TotalCredits, 2) : 0
            );
        }
    }
}
