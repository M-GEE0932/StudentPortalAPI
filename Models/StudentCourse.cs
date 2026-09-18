using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class StudentCourse
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey("StudentId")]
    public Student? Student { get; set; }

    public int CourseId { get; set; }
    [ForeignKey("CourseId")]
    public Course? Course { get; set; }

    public int? AcademicYearSemesterId { get; set; }
    [ForeignKey("AcademicYearSemesterId")]
    public AcademicYearSemester? AcademicYearSemester { get; set; }

    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? AssignmentScore { get; set; }
    public decimal? TotalScore { get; set; }

    [MaxLength(2)]
    public string? Grade { get; set; }

    public decimal? GradePoints { get; set; }

    public bool IsApproved { get; set; } = false;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
