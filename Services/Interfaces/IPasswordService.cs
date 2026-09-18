namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IPasswordService
{
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> AdminResetPasswordAsync(string email, string newPassword, int adminId);
}
