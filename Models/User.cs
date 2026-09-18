using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public enum UserRole
{
    Admin = 0,
    Faculty = 1,
    Student = 2
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PhotoUrl { get; set; }

    public bool IsActive { get; set; } = false;

    [Required]
    public UserRole Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    // Navigation properties – added for eager loading and repository queries
    public Student? Student { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<Notice> Notices { get; set; } = new List<Notice>();
}
