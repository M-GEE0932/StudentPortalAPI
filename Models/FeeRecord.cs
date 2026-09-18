using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPortalAPI.Models;

public enum FeeStatus
{
    Pending = 1,
    Paid = 2,
    Overdue = 3,
    Exempt = 4,
    PartiallyPaid = 5
}

public class FeeRecord
{
    [Key]
    public int Id { get; set; }

    public int StudentId { get; set; }
    [ForeignKey("StudentId")]
    public Student? Student { get; set; }

    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; } = 0;

    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }

    [Required]
    public FeeStatus Status { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? TransactionId { get; set; }

    [MaxLength(50)]
    public string? ExemptionType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
