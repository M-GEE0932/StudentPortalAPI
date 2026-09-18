using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<Student?> GetStudentWithUserAsync(int studentId);
        Task<Student?> GetStudentWithCoursesAsync(int studentId);
        Task<Student?> GetStudentByUserIdAsync(int userId);
        Task<IEnumerable<Student>> GetStudentsByDepartmentAsync(int departmentId);
        Task<IEnumerable<Student>> GetStudentsByCourseAsync(int courseId);
    }
}