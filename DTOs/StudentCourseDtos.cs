namespace StudentPortalAPI.DTOs;

public class StudentCourseDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentIdNumber { get; set; }
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
    public int Credits { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? AcademicYearSemesterId { get; set; }
    public string? AcademicYear { get; set; }
    public int? YearNumber { get; set; }
    public int? SemesterNumber { get; set; }
    public string? FacultyName { get; set; }
    public decimal? MidtermScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? TotalScore { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool IsApproved { get; set; }
    public DateTime EnrolledAt { get; set; }
}

public class AssignStudentCourseDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
}

public class BulkAssignDto
{
    public int StudentId { get; set; }
    public List<int> CourseIds { get; set; } = new();
}
