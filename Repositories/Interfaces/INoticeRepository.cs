using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface INoticeRepository : IGenericRepository<Notice>
    {
        Task<IEnumerable<Notice>> GetActiveNoticesAsync();
        Task<IEnumerable<Notice>> GetNoticesForUserAsync(int? departmentId, string targetType);
        Task<Notice?> GetNoticeWithDetailsAsync(int noticeId);
    }
}