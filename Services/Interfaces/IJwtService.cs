namespace StudentPortalAPI.Services;

using StudentPortalAPI.Models;

public interface IJwtService
{
    string GenerateToken(User user);
}
