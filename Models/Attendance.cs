using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3,
    Excused = 4
}

public class Attendance
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey("StudentId")]
    public Student? Student { get; set; }

    public int CourseId { get; set; }
    [ForeignKey("CourseId")]
    public Course? Course { get; set; }

    public int? FacultyId { get; set; }
    [ForeignKey("FacultyId")]
    public Faculty? Faculty { get; set; }

    public DateTime Date { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    [MaxLength(255)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
