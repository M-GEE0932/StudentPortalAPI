using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Helpers;

public static class JwtHelper
{
    public static string GenerateToken(User user, IConfiguration configuration)
    {
        //  Secret key from appsettings.json
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)
        );

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        //  Claims included in the JWT
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // used in controllers
            new Claim(ClaimTypes.Role, user.Role.ToString()),           // role-based logic
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("IsActive", user.IsActive.ToString())
        };

        //  Build the token
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(configuration["Jwt:ExpiryInMinutes"] ?? "1440")
            ),
            signingCredentials: credentials
        );

        // Return token string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
