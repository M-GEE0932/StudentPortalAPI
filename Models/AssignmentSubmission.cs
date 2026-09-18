using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public class AssignmentSubmission
{
    [Key]
    public int Id { get; set; }

    public int AssignmentId { get; set; }
    [ForeignKey("AssignmentId")]
    public Assignment? Assignment { get; set; }

    public int StudentId { get; set; }
    [ForeignKey("StudentId")]
    public Student? Student { get; set; }

    [Required, MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Comments { get; set; }

    public decimal? Score { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? GradedAt { get; set; }
}
