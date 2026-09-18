using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Notification>> GetByUserAsync(string userId)
            => await Query()
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(string userId)
            => await Query()
                .Where(n => n.UserId == userId && !n.Read && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

        public async Task MarkAsReadAsync(int id)
        {
            var notification = await GetByIdAsync(id);
            if (notification != null)
            {
                notification.Read = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> ExecuteMarkAllReadAsync(string userId)
            => await Query()
                .Where(n => n.UserId == userId && !n.Read && !n.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true));

        public async Task<int> ExecuteDeleteOlderThanAsync(DateTime cutoff)
            => await Query()
                .Where(n => n.CreatedAt < cutoff && n.IsDeleted)
                .ExecuteDeleteAsync();
    }
}
