using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class AttendanceRepository : GenericRepository<Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext context) : base(context) { }

        // Get attendances by student only
        public async Task<IEnumerable<Attendance>> GetByStudentAsync(int studentId)
            => await Query()
                .Where(a => a.StudentId == studentId)
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Course)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

        public async Task<IEnumerable<Attendance>> GetByStudentAndCourseAsync(int studentId, int courseId)
            => await Query()
                .Where(a => a.StudentId == studentId && a.CourseId == courseId)
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Course)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

        public async Task<IEnumerable<Attendance>> GetByCourseAndDateAsync(int courseId, DateTime date)
            => await Query()
                .Where(a => a.CourseId == courseId && a.Date.Date == date.Date)
                .Include(a => a.Student).ThenInclude(s => s!.User)
                .Include(a => a.Course)
                .ToListAsync();

        public async Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId)
        {
            var total = await Query().CountAsync(a => a.StudentId == studentId && a.CourseId == courseId);
            if (total == 0) return 100;

            var present = await Query().CountAsync(a =>
                a.StudentId == studentId && a.CourseId == courseId &&
                (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.Excused));

            return (decimal)present / total * 100;
        }

        public async Task<Attendance?> GetExistingAttendanceAsync(int studentId, int courseId, DateTime date)
            => await Query()
                .FirstOrDefaultAsync(a => a.StudentId == studentId
                    && a.CourseId == courseId
                    && a.Date.Date == date.Date);
    }
}
