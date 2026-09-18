using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
            => await _dbSet.FirstOrDefaultAsync(u => u.Email == email.ToLower());

        public async Task<User?> GetUserWithRoleDataAsync(string email)
            => await _dbSet
                .Include(u => u.Student)
                .Include(u => u.Faculty)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());

        public async Task<IEnumerable<User>> GetPendingUsersAsync()
            => await _dbSet.Where(u => !u.IsActive).OrderByDescending(u => u.CreatedAt).ToListAsync();

        public async Task<bool> EmailExistsAsync(string email)
            => await _dbSet.AnyAsync(u => u.Email == email.ToLower());
    }
}