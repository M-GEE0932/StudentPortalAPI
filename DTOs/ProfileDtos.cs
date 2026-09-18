namespace StudentPortalAPI.DTOs;

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public StudentProfileDto? StudentProfile { get; set; }
    public FacultyProfileDto? FacultyProfile { get; set; }
}

public class StudentProfileDto
{
    public string? StudentIdNumber { get; set; }
    public string? DepartmentName { get; set; }
    public decimal CGPA { get; set; }
    public int TotalCreditsEarned { get; set; }
    public int CurrentYear { get; set; } = 1;
    public int CurrentSemester { get; set; } = 1;
    public DateTime EnrollmentDate { get; set; }
    public bool IsGraduated { get; set; }
    public int EnrolledCourses { get; set; }
    public int ApprovedCourses { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int PendingResults { get; set; }
    public decimal TotalFees { get; set; }
    public decimal PaidFees { get; set; }
    public decimal OverdueFees { get; set; }
    public string FeeStatus { get; set; } = "No Fees";
}

public class FacultyProfileDto
{
    public string? FacultyIdNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string? Title { get; set; }
    public DateTime HireDate { get; set; }
    public int CourseCount { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? PhotoUrl { get; set; }
}
