namespace StudentPortalAPI.DTOs;

public class DepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationYears { get; set; }
    public int StudentCount { get; set; }
    public int FacultyCount { get; set; }
    public int CourseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationYears { get; set; } = 4;
}

public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? DurationYears { get; set; }
}

public class AcademicYearSemesterDto
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public int YearNumber { get; set; }
    public int SemesterNumber { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateAcademicYearSemesterRequest
{
    public int DepartmentId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
}
