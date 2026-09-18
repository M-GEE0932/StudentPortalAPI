namespace StudentPortalAPI.Services;

using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

public class PasswordService : IPasswordService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public PasswordService(
        IUserRepository userRepository,
        IAuditService auditService,
        INotificationService notificationService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _auditService = auditService;
        _notificationService = notificationService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (!_passwordHasher.VerifyPassword(request.OldPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect");

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        if (userId > 0)
        {
            await _notificationService.NotifyUserAsync(userId, "Password Changed", "Your password has been changed successfully.", "success");
        }

        return true;
    }

    public async Task<bool> AdminResetPasswordAsync(string email, string newPassword, int adminId)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("User not found");

        var oldValues = new { PasswordHash = user.PasswordHash };

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(
            adminId,
            "ResetPassword",
            "User",
            user.Id,
            oldValues,
            new { PasswordHash = user.PasswordHash }
        );

        if (user.Id > 0)
        {
            await _notificationService.NotifyUserAsync(user.Id, "Password Reset", "Your password has been reset by an administrator.", "warning");
        }

        await _notificationService.NotifyUserAsync(adminId, "Password Reset", $"You reset the password for {user.FullName} ({user.Email}).", "success");

        return true;
    }
}
