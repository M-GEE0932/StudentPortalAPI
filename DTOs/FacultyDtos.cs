namespace StudentPortalAPI.DTOs;

public class FacultyDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyIdNumber { get; set; }
    public string? Title { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
    public int CourseCount { get; set; }
}

public class CreateFacultyDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public int DepartmentId { get; set; }
    public string? Title { get; set; }
}

public class UpdateFacultyDto
{
    public int? DepartmentId { get; set; }
    public string? Title { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignFacultyRequest
{
    public int FacultyId { get; set; }
    public int DepartmentId { get; set; }
    public string? Title { get; set; }
}

