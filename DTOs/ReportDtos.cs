namespace StudentPortalAPI.DTOs;

public class DashboardStatsDto
{
    public int TotalStudents { get; set; }
    public int TotalFaculty { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalCourses { get; set; }
    public int PendingApprovals { get; set; }
    public int PendingResults { get; set; }
    public decimal FeeCollectionRate { get; set; }
    public decimal TotalFees { get; set; }
    public decimal CollectedFees { get; set; }
    public decimal OverdueFees { get; set; }
}

public class StudentDashboardDto
{
    public string StudentName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public decimal CGPA { get; set; }
    public int EnrolledCourses { get; set; }
    public int ApprovedCourses { get; set; }
    public int TotalCredits { get; set; }
    public int PendingAssignments { get; set; }
    public decimal AttendancePercentage { get; set; }
    public decimal FeeBalance { get; set; }
    public List<CourseSummaryDto> RecentCourses { get; set; } = new();
}

public class FacultyDashboardDto
{
    public string FacultyName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int TotalCourses { get; set; }
    public int TotalStudents { get; set; }
    public int PendingSubmissions { get; set; }
    public int PendingResults { get; set; }
}

public class CourseSummaryDto
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public string? Grade { get; set; }
    public decimal AttendancePercentage { get; set; }
}
