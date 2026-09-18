namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IReportService
{
    Task<DashboardStatsDto> GetAdminDashboardAsync();
    Task<StudentDashboardDto?> GetStudentDashboardAsync(int studentUserId);
    Task<FacultyDashboardDto?> GetFacultyDashboardAsync(int facultyUserId);
}
