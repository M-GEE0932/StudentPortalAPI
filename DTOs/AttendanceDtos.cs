namespace StudentPortalAPI.DTOs;

public class AttendanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

public class MarkAttendanceRequest
{
    public int CourseId { get; set; }
    public DateTime Date { get; set; }
    public List<AttendanceEntry> Entries { get; set; } = new();
}

public class AttendanceEntry
{
    public int StudentId { get; set; }
    public string Status { get; set; } = "Present";
    public string? Remarks { get; set; }
}

public class AttendanceSummaryDto
{
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public int TotalClasses { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int ExcusedCount { get; set; }
    public decimal AttendancePercentage { get; set; }
}
