namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IAttendanceService
{
    Task<List<AttendanceDto>> GetByCourseAndDateAsync(int courseId, DateTime date);
    Task<List<AttendanceDto>> GetByStudentAsync(int studentId);
    Task<List<AttendanceDto>> GetByStudentAndCourseAsync(int studentId, int courseId);
    Task<List<AttendanceSummaryDto>> GetSummaryByCourseAsync(int courseId);
    Task<AttendanceSummaryDto?> GetSummaryByStudentAndCourseAsync(int studentId, int courseId);
    Task<bool> MarkAttendanceAsync(MarkAttendanceRequest request, int facultyUserId);
    Task<decimal> GetAttendancePercentageAsync(int studentId, int courseId);
}
