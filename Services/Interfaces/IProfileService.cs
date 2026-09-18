namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(int userId);
    Task<ProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest request, int actorUserId);
}
