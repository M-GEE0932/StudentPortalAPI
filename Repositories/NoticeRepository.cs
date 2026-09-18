using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class NoticeRepository : GenericRepository<Notice>, INoticeRepository
    {
        public NoticeRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Notice>> GetActiveNoticesAsync()
            => await Query()
                .Where(n => n.IsActive)
                .Include(n => n.PostedByUser)
                .Include(n => n.TargetDepartment)
                .Include(n => n.TargetCourse)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Notice>> GetNoticesForUserAsync(int? departmentId, string targetType)
        {
            var query = Query().Where(n => n.IsActive);

            if (targetType == "All")
                query = query.Where(n => n.TargetType == NoticeTargetType.All
                    || (n.TargetType == NoticeTargetType.Department && n.TargetDepartmentId == departmentId));
            else if (targetType == "Department")
                query = query.Where(n => n.TargetType == NoticeTargetType.All
                    || n.TargetDepartmentId == departmentId);

            return await query
                .Include(n => n.PostedByUser)
                .Include(n => n.TargetDepartment)
                .Include(n => n.TargetCourse)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notice?> GetNoticeWithDetailsAsync(int noticeId)
            => await Query()
                .Where(n => n.Id == noticeId)
                .Include(n => n.PostedByUser)
                .Include(n => n.TargetDepartment)
                .Include(n => n.TargetCourse)
                .FirstOrDefaultAsync();
    }
}
