namespace StudentPortalAPI.DTOs;

public class StudentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? StudentIdNumber { get; set; }
    public int CurrentYear { get; set; }
    public int CurrentSemester { get; set; }
    public decimal CGPA { get; set; }
    public int TotalCreditsEarned { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public bool IsGraduated { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignStudentRequest
{
    public int StudentId { get; set; }
    public int DepartmentId { get; set; }
    public int CurrentYear { get; set; } = 1;
    public int CurrentSemester { get; set; } = 1;
    public List<int> CourseIds { get; set; } = new();
    public bool? IsActive { get; set; }
}

public class EnrollCourseRequest
{
    public int CourseId { get; set; }
    public int AcademicYearSemesterId { get; set; }
}
