using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public enum NoticeTargetType
{
    All = 0,
    Department = 1,
    Course = 2,
    AcademicYearSemester = 3
}

public class Notice
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public int? PostedByUserId { get; set; }
    [ForeignKey("PostedByUserId")]
    public User? PostedByUser { get; set; }

    [Required]
    public NoticeTargetType TargetType { get; set; }

    public int? TargetDepartmentId { get; set; }
    [ForeignKey("TargetDepartmentId")]
    public Department? TargetDepartment { get; set; }

    public int? TargetCourseId { get; set; }
    [ForeignKey("TargetCourseId")]
    public Course? TargetCourse { get; set; }

    public int? TargetAcademicYearSemesterId { get; set; }
    [ForeignKey("TargetAcademicYearSemesterId")]
    public AcademicYearSemester? TargetAcademicYearSemester { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}
