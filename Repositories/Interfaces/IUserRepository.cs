using StudentPortalAPI.Models;

namespace StudentPortalAPI.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetUserWithRoleDataAsync(string email);
        Task<IEnumerable<User>> GetPendingUsersAsync();
        Task<bool> EmailExistsAsync(string email);
    }
}