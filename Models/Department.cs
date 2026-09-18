using System.ComponentModel.DataAnnotations;

namespace StudentPortalAPI.Models;

public class Department
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DurationYears { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AcademicYearSemester> AcademicYearSemesters { get; set; } = new List<AcademicYearSemester>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
}
