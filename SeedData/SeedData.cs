using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.SeedData;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

        if (!await context.Users.AnyAsync())
        {
            var adminUser = new User
            {
                Email = "admin@portal.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Strong@123"),
                FullName = "Mr. Administrator",
                IsActive = true,
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
