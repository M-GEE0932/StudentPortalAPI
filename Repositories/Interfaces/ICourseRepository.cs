using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        Task<Course?> GetCourseWithDetailsAsync(int courseId);
        Task<IEnumerable<Course>> GetCoursesByYearSemesterAsync(int yearSemesterId);
        Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(int departmentId);
        Task<IEnumerable<Course>> GetCoursesByFacultyAsync(int facultyId);
        Task<IEnumerable<Course>> GetCoursesByStudentAsync(int studentId);
        Task<bool> CourseCodeExistsAsync(string code, int? excludeId = null);
    }
}