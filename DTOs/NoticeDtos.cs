namespace StudentPortalAPI.DTOs;

public class NoticeDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int? PostedByUserId { get; set; }
    public string? PostedByName { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int? TargetDepartmentId { get; set; }
    public string? TargetDepartmentName { get; set; }
    public int? TargetCourseId { get; set; }
    public string? TargetCourseName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateNoticeRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string TargetType { get; set; } = "All";
    public int? TargetDepartmentId { get; set; }
    public int? TargetCourseId { get; set; }
    public int? TargetAcademicYearSemesterId { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
