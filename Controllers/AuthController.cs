using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IPasswordService _passwordService;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IPasswordService passwordService)
    {
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _passwordService = passwordService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found or invalid in token.");
        return userId;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authenticationService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authenticationService.LoginAsync(request);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<ActionResult> GetPendingUsers()
    {
        var users = await _userManagementService.GetPendingUsersAsync();
        var safeDtos = users.Select(u => new
        {
            id = u.Id,
            email = u.Email,
            fullName = u.FullName,
            photoUrl = u.PhotoUrl,
            isActive = u.IsActive,
            role = u.Role.ToString(),
            createdAt = u.CreatedAt,
            updatedAt = u.UpdatedAt
        }).ToList();
        return Ok(safeDtos);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("approve/{userId}")]
    public async Task<ActionResult> ApproveUser(int userId)
    {
        var adminUserId = GetCurrentUserId();
        await _userManagementService.ApproveUserAsync(userId, adminUserId);
        return Ok(new { message = "User approved successfully" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("deactivate/{userId}")]
    public async Task<ActionResult> DeactivateUser(int userId)
    {
        var adminUserId = GetCurrentUserId();
        await _userManagementService.DeactivateUserAsync(userId, adminUserId);
        return Ok(new { message = "User deactivated" });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin-reset")]
    public async Task<IActionResult> AdminReset([FromBody] ResetRequest req)
    {
        var adminId = GetCurrentUserId();
        await _passwordService.AdminResetPasswordAsync(req.Email, req.NewPassword, adminId);
        return Ok(new { message = "Password reset successful" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        await _passwordService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Password changed successfully" });
    }
}
