using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IAttendanceRepository : IGenericRepository<Attendance>
    {
        Task<IEnumerable<Attendance>> GetByStudentAndCourseAsync(int studentId, int courseId);
        Task<IEnumerable<Attendance>> GetByStudentAsync(int studentId);
        Task<IEnumerable<Attendance>> GetByCourseAndDateAsync(int courseId, DateTime date);
        Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId);
        Task<Attendance?> GetExistingAttendanceAsync(int studentId, int courseId, DateTime date);
    }
}