using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class AssignmentRepository : GenericRepository<Assignment>, IAssignmentRepository
    {
        public AssignmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Assignment>> GetByCourseAsync(int courseId)
            => await Query()
                .Where(a => a.CourseId == courseId)
                .Include(a => a.Course)
                .Include(a => a.Faculty).ThenInclude(f => f!.User)
                .Include(a => a.Submissions)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Assignment>> GetByStudentAsync(int studentId)
        {
            var courseIds = await Query()
                .Where(a => a.Submissions.Any(s => s.StudentId == studentId))
                .Select(a => a.CourseId)
                .ToListAsync();

            return await Query()
                .Where(a => courseIds.Contains(a.CourseId))
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Assignment?> GetAssignmentWithDetailsAsync(int assignmentId)
            => await Query()
                .Where(a => a.Id == assignmentId)
                .Include(a => a.Course)
                .Include(a => a.Faculty).ThenInclude(f => f!.User)
                .Include(a => a.Submissions)
                .FirstOrDefaultAsync();
    }
}
