using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class Course
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int Credits { get; set; }

    public int DepartmentId { get; set; }
    [ForeignKey("DepartmentId")]
    public Department? Department { get; set; }

    public int AcademicYearSemesterId { get; set; }
    [ForeignKey("AcademicYearSemesterId")]
    public AcademicYearSemester? AcademicYearSemester { get; set; }

    public int? FacultyId { get; set; }
    [ForeignKey("FacultyId")]
    public Faculty? Faculty { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
