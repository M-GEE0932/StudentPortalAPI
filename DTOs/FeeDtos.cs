namespace StudentPortalAPI.DTOs;

public class FeeRecordDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance => Amount - AmountPaid;
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? TransactionId { get; set; }
    public string? ExemptionType { get; set; }
}

public class CreateFeeRecordRequest
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string? Description { get; set; }
    public string? ExemptionType { get; set; }
}

public class PayFeeRequest
{
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
}

public class ExemptRequest
{
    public string ExemptionType { get; set; } = string.Empty;
}

public class FeeDashboardDto
{
    public decimal TotalCollected { get; set; }
    public decimal TotalPending { get; set; }
    public decimal TotalOverdue { get; set; }
    public int TotalStudents { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int OverdueCount { get; set; }
    public int ExemptCount { get; set; }
    public int PartiallyPaidCount { get; set; }
}
