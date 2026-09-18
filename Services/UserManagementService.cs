namespace StudentPortalAPI.Services;

using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Models;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public UserManagementService(
        IUserRepository userRepository,
        IAuditService auditService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ApproveUserAsync(int userId, int adminUserId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogAsync(adminUserId, "ApproveUser", "User", userId);

        if (userId > 0)
        {
            await _notificationService.NotifyUserAsync(userId, "Account Approved",
                "Your account has been approved. You can now log in.", "success");
        }

        await _notificationService.NotifyUserAsync(adminUserId, "User Approved",
            $"You approved the account of {user.FullName} ({user.Email}).", "success");

        return true;
    }

    public async Task<bool> DeactivateUserAsync(int userId, int adminUserId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        if (userId > 0)
        {
            await _notificationService.NotifyUserAsync(userId, "Account Deactivated",
                "Your account has been deactivated. Contact an administrator.", "warning");
        }

        await _notificationService.NotifyUserAsync(adminUserId, "User Deactivated",
            $"You deactivated the account of {user.FullName} ({user.Email}).", "info");

        return true;
    }

    public async Task<List<User>> GetPendingUsersAsync()
        => (await _userRepository.GetPendingUsersAsync()).ToList();

    public async Task<User?> GetUserByIdAsync(int userId)
        => await _userRepository.GetByIdAsync(userId);

    public async Task<User?> GetUserByEmailAsync(string email)
        => await _userRepository.GetByEmailAsync(email);

    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
