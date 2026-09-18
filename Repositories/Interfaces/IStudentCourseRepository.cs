using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IStudentCourseRepository : IGenericRepository<StudentCourse>
    {
        Task<IEnumerable<StudentCourse>> GetByStudentAsync(int studentId);
        Task<IEnumerable<StudentCourse>> GetByCourseAsync(int courseId);
        Task<StudentCourse?> GetByStudentAndCourseAsync(int studentId, int courseId);
        Task<bool> IsEnrolledAsync(int studentId, int courseId);
        Task<bool> ExistsAsync(int studentId, int courseId);
        Task<StudentCourse?> GetByIdWithDetailsAsync(int enrollmentId);
        Task<IEnumerable<StudentCourse>> GetEnrolledWithDetailsAsync(int studentId);
    }
}