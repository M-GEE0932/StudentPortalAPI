namespace StudentPortalAPI.DTOs;

public class AssignmentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public string? FileUrl { get; set; }
    public DateTime DueDate { get; set; }
    public int MaxScore { get; set; }
    public int SubmittedCount { get; set; }
    public int TotalStudents { get; set; }
    public bool HasSubmitted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAssignmentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CourseId { get; set; }
    public string? FileUrl { get; set; }
    public DateTime DueDate { get; set; }
    public int MaxScore { get; set; } = 100;
}

public class SubmitAssignmentRequest
{
    public string FileUrl { get; set; } = string.Empty;
    public string? Comments { get; set; }
}

public class GradeSubmissionRequest
{
    public decimal Score { get; set; }
}

public class AssignmentSubmissionDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string? AssignmentTitle { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentIdNumber { get; set; }
    public string? StudentEmail { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public decimal? Score { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}
