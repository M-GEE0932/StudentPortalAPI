using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class Faculty
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public int? DepartmentId { get; set; }
    [ForeignKey("DepartmentId")]
    public Department? Department { get; set; }

    [MaxLength(20)]
    public string? FacultyId { get; set; }

    [MaxLength(50)]
    public string? Title { get; set; }

    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
