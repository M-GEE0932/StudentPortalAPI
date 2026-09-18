using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<IEnumerable<Notification>> GetByUserAsync(string userId);
        Task<IEnumerable<Notification>> GetUnreadByUserAsync(string userId);
        Task MarkAsReadAsync(int id);
        Task<int> ExecuteMarkAllReadAsync(string userId);
        Task<int> ExecuteDeleteOlderThanAsync(DateTime cutoff);
    }
}
