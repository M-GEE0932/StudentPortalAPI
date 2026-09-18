namespace StudentPortalAPI.Services;

using StudentPortalAPI.DTOs;

public interface IFeeService
{
    Task<List<FeeRecordDto>> GetByStudentAsync(int studentId);
    Task<List<FeeRecordDto>> GetAllAsync();
    Task<PagedResult<FeeRecordDto>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 10);
    Task<FeeDashboardDto> GetDashboardAsync();
    Task<FeeRecordDto> CreateAsync(CreateFeeRecordRequest request, int actorUserId);
    Task<FeeRecordDto?> PayAsync(int feeId, PayFeeRequest request, int actorUserId);
    Task<bool> ExemptAsync(int feeId, string exemptionType, int actorUserId);
    Task<FeeRecordDto?> GetByIdAsync(int id);
    Task<decimal> GetStudentBalanceAsync(int studentId);
    Task<bool> HasOverdueFeesAsync(int studentId);
}
