using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class Student
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
    public string? StudentId { get; set; }

    public int CurrentYear { get; set; } = 1;
    public int CurrentSemester { get; set; } = 1;

    public decimal CGPA { get; set; } = 0;
    public int TotalCreditsEarned { get; set; } = 0;

    public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    public DateTime? GraduationDate { get; set; }
    public bool IsGraduated { get; set; } = false;

    public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; set; } = new List<AssignmentSubmission>();
    public ICollection<FeeRecord> FeeRecords { get; set; } = new List<FeeRecord>();
}
