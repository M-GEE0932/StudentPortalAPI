using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class AssignmentSubmissionRepository : GenericRepository<AssignmentSubmission>, IAssignmentSubmissionRepository
    {
        public AssignmentSubmissionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<AssignmentSubmission>> GetByAssignmentAsync(int assignmentId)
            => await Query()
                .Where(s => s.AssignmentId == assignmentId)
                .Include(s => s.Student).ThenInclude(st => st!.User)
                .Include(s => s.Assignment)
                .ToListAsync();

        public async Task<AssignmentSubmission?> GetSubmissionWithDetailsAsync(int submissionId)
            => await Query()
                .Where(s => s.Id == submissionId)
                .Include(s => s.Student).ThenInclude(st => st!.User)
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync();

        public async Task<bool> HasStudentSubmittedAsync(int assignmentId, int studentId)
            => await Query().AnyAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

        public async Task<AssignmentSubmission?> GetStudentSubmissionAsync(int assignmentId, int studentId)
            => await Query()
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
    }
}
