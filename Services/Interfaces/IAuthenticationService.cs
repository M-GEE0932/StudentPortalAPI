namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IAuthenticationService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
