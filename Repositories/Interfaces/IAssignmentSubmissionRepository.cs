using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IAssignmentSubmissionRepository : IGenericRepository<AssignmentSubmission>
    {
        Task<IEnumerable<AssignmentSubmission>> GetByAssignmentAsync(int assignmentId);
        Task<AssignmentSubmission?> GetSubmissionWithDetailsAsync(int submissionId);
        Task<bool> HasStudentSubmittedAsync(int assignmentId, int studentId);
        Task<AssignmentSubmission?> GetStudentSubmissionAsync(int assignmentId, int studentId);
    }
}