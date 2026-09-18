using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IAssignmentRepository : IGenericRepository<Assignment>
    {
        Task<IEnumerable<Assignment>> GetByCourseAsync(int courseId);
        Task<IEnumerable<Assignment>> GetByStudentAsync(int studentId);
        Task<Assignment?> GetAssignmentWithDetailsAsync(int assignmentId);
    }
}