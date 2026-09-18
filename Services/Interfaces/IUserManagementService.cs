namespace StudentPortalAPI.Services;

using StudentPortalAPI.Models;

public interface IUserManagementService
{
    Task<bool> ApproveUserAsync(int userId, int adminUserId);
    Task<bool> DeactivateUserAsync(int userId, int adminUserId);
    Task<List<User>> GetPendingUsersAsync();
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task UpdateUserAsync(User user);
}
