namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IAssignmentService
{
    Task<List<AssignmentDto>> GetByCourseAsync(int courseId);
    Task<List<AssignmentDto>> GetForStudentAsync(int studentId);
    Task<AssignmentDto?> GetByIdAsync(int id);
    Task<AssignmentDto> CreateAsync(CreateAssignmentRequest request, int facultyUserId);
    Task<bool> SubmitAsync(int assignmentId, int studentUserId, SubmitAssignmentRequest request);
    Task<bool> GradeAsync(int submissionId, GradeSubmissionRequest request);
    Task<List<AssignmentSubmissionDto>> GetSubmissionsByAssignmentAsync(int assignmentId);
    Task<AssignmentSubmissionDto?> GetSubmissionByIdAsync(int submissionId);
    Task<bool> HasStudentSubmittedAsync(int assignmentId, int studentId);
    Task<bool> CheckAttendanceForSubmissionAsync(int studentId, int courseId);
}
