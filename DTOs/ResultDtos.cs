namespace StudentPortalAPI.DTOs;

public class ResultDto
{
    public int Id { get; set; }
    public int StudentCourseId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? AssignmentScore { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool IsApproved { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public string? DepartmentName { get; set; }
    public int? YearNumber { get; set; }
    public int? SemesterNumber { get; set; }
    public string? AcademicYear { get; set; }
}

public class EnterResultRequest
{
    public int StudentCourseId { get; set; }
    public decimal AssignmentScore { get; set; }
    public decimal MidtermScore { get; set; }
    public decimal FinalScore { get; set; }
}

public class ApproveResultRequest
{
    public int StudentCourseId { get; set; }
    public string Status { get; set; } = "Approved";
    public string? Remarks { get; set; }
}

public class TranscriptDto
{
    public string StudentName { get; set; } = string.Empty;
    public string? StudentIdNumber { get; set; }
    public string? DepartmentName { get; set; }
    public decimal CGPA { get; set; }
    public int TotalCreditsEarned { get; set; }
    public List<TranscriptEntryDto> Entries { get; set; } = new();
}

public class TranscriptEntryDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int Credits { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? AssignmentScore { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoints { get; set; }
    public int YearNumber { get; set; }
    public int SemesterNumber { get; set; }
    public string? AcademicYear { get; set; }
}
