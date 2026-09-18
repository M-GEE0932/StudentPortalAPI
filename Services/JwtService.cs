using StudentPortalAPI.Helpers;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user) => JwtHelper.GenerateToken(user, _configuration);
}
