namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IResultService
{
    Task<List<ResultDto>> GetByStudentAsync(int studentId);
    Task<List<ResultDto>> GetByCourseAsync(int courseId);
    Task<List<ResultDto>> GetPendingApprovalsAsync();
    Task<ResultDto?> GetByIdAsync(int studentCourseId);
    Task<bool> EnterResultAsync(EnterResultRequest request, int facultyUserId);
    Task<bool> ApproveResultAsync(ApproveResultRequest request, int adminUserId);
    Task<TranscriptDto?> GetTranscriptAsync(int studentId);
    Task<TranscriptDto?> GetTranscriptByYearSemesterAsync(int studentId, int? yearSemesterId);
    Task<bool> CanPrintTranscriptAsync(int studentId);
}
