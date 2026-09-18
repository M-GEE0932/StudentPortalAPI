using StudentPortalAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IResultRepository : IGenericRepository<StudentCourse>
    {
        Task<IEnumerable<StudentCourse>> GetResultsByStudentAsync(int studentId);
        Task<IEnumerable<StudentCourse>> GetResultsByCourseAsync(int courseId);
        Task<IEnumerable<StudentCourse>> GetPendingApprovalsAsync();
        Task<StudentCourse?> GetStudentCourseWithDetailsAsync(int studentCourseId);
        Task<decimal> GetStudentCGPAAsync(int studentId);
        Task UpdateStudentCGPAAsync(int studentId);
        Task<Dictionary<int, decimal>> GetStudentCGPAListAsync(IEnumerable<int> studentIds);
    }
}