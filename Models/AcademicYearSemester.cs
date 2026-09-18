using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class AcademicYearSemester
{
    [Key]
    public int Id { get; set; }

    public int DepartmentId { get; set; }
    [ForeignKey("DepartmentId")]
    public Department? Department { get; set; }

    public int YearNumber { get; set; }
    public int SemesterNumber { get; set; }

    [Required, MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
}
