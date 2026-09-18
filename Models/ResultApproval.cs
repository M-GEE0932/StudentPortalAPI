using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class ResultApproval
{
    [Key]
    public int Id { get; set; }

    public int StudentCourseId { get; set; }
    [ForeignKey("StudentCourseId")]
    public StudentCourse? StudentCourse { get; set; }

    public int? ApprovedByUserId { get; set; }
    [ForeignKey("ApprovedByUserId")]
    public User? ApprovedByUser { get; set; }

    [Required]
    public ApprovalStatus Status { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
