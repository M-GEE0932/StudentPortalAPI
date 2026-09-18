namespace StudentPortalAPI.DTOs;

public class CourseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int AcademicYearSemesterId { get; set; }
    public string? AcademicYear { get; set; }
    public int? YearNumber { get; set; }
    public int? SemesterNumber { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public string? Description { get; set; }
    public int EnrolledStudents { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCourseRequest
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int DepartmentId { get; set; }
    public int AcademicYearSemesterId { get; set; }
    public int? FacultyId { get; set; }
    public string? Description { get; set; }
}

public class UpdateCourseRequest
{
    public string? Code { get; set; }
    public string? Title { get; set; }
    public int? Credits { get; set; }
    public int? DepartmentId { get; set; }
    public int? AcademicYearSemesterId { get; set; }
    public int? FacultyId { get; set; }
    public string? Description { get; set; }
}

public class AssignCourseFacultyRequest
{
    public int FacultyId { get; set; }
}
