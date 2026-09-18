using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class Assignment
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int CourseId { get; set; }
    [ForeignKey("CourseId")]
    public Course? Course { get; set; }

    public int? FacultyId { get; set; }
    [ForeignKey("FacultyId")]
    public Faculty? Faculty { get; set; }

    [MaxLength(500)]
    public string? FileUrl { get; set; }

    public DateTime DueDate { get; set; }
    public int MaxScore { get; set; } = 100;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
