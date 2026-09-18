using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class FeeService : IFeeService
{
    private readonly IFeeRepository _feeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public FeeService(
        IFeeRepository feeRepository,
        IStudentRepository studentRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        INotificationService notificationService)
    {
        _feeRepository = feeRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<List<FeeRecordDto>> GetByStudentAsync(int studentId)
    {
        return await _feeRepository.Query()
            .Where(f => f.StudentId == studentId)
            .Include(f => f.Student).ThenInclude(s => s!.User)
            .OrderByDescending(f => f.DueDate)
            .Select(f => new FeeRecordDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                StudentName = f.Student!.User!.FullName,
                Amount = f.Amount,
                AmountPaid = f.AmountPaid,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status.ToString(),
                Description = f.Description,
                TransactionId = f.TransactionId,
                ExemptionType = f.ExemptionType
            }).ToListAsync();
    }

    public async Task<List<FeeRecordDto>> GetAllAsync()
    {
        return await _feeRepository.Query()
            .Include(f => f.Student).ThenInclude(s => s!.User)
            .OrderByDescending(f => f.DueDate)
            .Select(f => new FeeRecordDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                StudentName = f.Student!.User!.FullName,
                Amount = f.Amount,
                AmountPaid = f.AmountPaid,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status.ToString(),
                Description = f.Description,
                TransactionId = f.TransactionId,
                ExemptionType = f.ExemptionType
            }).ToListAsync();
    }

    public async Task<FeeDashboardDto> GetDashboardAsync()
    {
        var records = await _feeRepository.Query().ToListAsync();

        return new FeeDashboardDto
        {
            //  Sum ALL AmountPaid from ALL records
            // This includes Overdue records with partial payments
            // Aligns with Admin Dashboard logic (ReportService.GetAdminDashboardAsync)
            TotalCollected = records.Sum(f => f.AmountPaid),

            TotalPending = records
                .Where(f => f.Status == FeeStatus.Pending)
                .Sum(f => f.Amount - f.AmountPaid),

            TotalOverdue = records
                .Where(f => f.Status == FeeStatus.Overdue)
                .Sum(f => f.Amount - f.AmountPaid),

            TotalStudents = await _studentRepository.Query().CountAsync(),
            PaidCount = records.Count(f => f.Status == FeeStatus.Paid),
            PendingCount = records.Count(f => f.Status == FeeStatus.Pending),
            OverdueCount = records.Count(f => f.Status == FeeStatus.Overdue),
            ExemptCount = records.Count(f => f.Status == FeeStatus.Exempt),
            PartiallyPaidCount = records.Count(f => f.Status == FeeStatus.PartiallyPaid)
        };
    }

    public async Task<FeeRecordDto> CreateAsync(CreateFeeRecordRequest request, int actorUserId)
    {
        var record = new FeeRecord
        {
            StudentId = request.StudentId,
            Amount = request.Amount,
            DueDate = request.DueDate,
            Description = request.Description,
            ExemptionType = request.ExemptionType,
            Status = FeeStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _feeRepository.Add(record);
        await _unitOfWork.SaveChangesAsync();

        var student = await _studentRepository.Query().Include(s => s.User).FirstOrDefaultAsync(s => s.Id == record.StudentId);

        // Notify the student (PERSISTED)
        if (student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                student.UserId,
                "New Fee Record",
                $"New fee record created: {request.Description} - Amount {request.Amount:C}",
                "info",
                "/student/fees");
        }

        // Notify the admin (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Fee Created",
            $"You created a fee record for {student?.User?.FullName ?? "student"}: {request.Description}",
            "success");

        return new FeeRecordDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = student?.User?.FullName,
            Amount = record.Amount,
            AmountPaid = record.AmountPaid,
            DueDate = record.DueDate,
            Status = record.Status.ToString(),
            Description = record.Description
        };
    }

    public async Task<FeeRecordDto?> PayAsync(int feeId, PayFeeRequest request, int actorUserId)
    {
        var record = await _feeRepository.Query().FirstOrDefaultAsync(f => f.Id == feeId);
        if (record == null) return null;

        record.AmountPaid += request.Amount;
        if (!string.IsNullOrWhiteSpace(request.TransactionId))
            record.TransactionId = request.TransactionId;
        record.PaidDate = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        if (record.AmountPaid >= record.Amount)
            record.Status = FeeStatus.Paid;
        else if (record.DueDate < DateTime.UtcNow)
            record.Status = FeeStatus.Overdue;
        else
            record.Status = FeeStatus.PartiallyPaid;

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogAsync(null, "FeePayment", "FeeRecord", feeId, newValues: new { request.Amount, request.TransactionId });

        var student = await _studentRepository.Query().Include(s => s.User).FirstOrDefaultAsync(s => s.Id == record.StudentId);

        // Notify the student (PERSISTED)
        if (student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                student.UserId,
                "Payment Received",
                $"Payment of {request.Amount} ETB received for {record.Description}. New status: {record.Status}",
                "success",
                "/student/fees");
        }

        // Notify the admin (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Payment Recorded",
            $"You recorded a payment of {request.Amount:C} for {student?.User?.FullName ?? "student"} (Fee: {record.Description})",
            "success");

        return new FeeRecordDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = student?.User?.FullName,
            Amount = record.Amount,
            AmountPaid = record.AmountPaid,
            DueDate = record.DueDate,
            PaidDate = record.PaidDate,
            Status = record.Status.ToString(),
            Description = record.Description,
            TransactionId = record.TransactionId
        };
    }

    public async Task<bool> ExemptAsync(int feeId, string exemptionType, int actorUserId)
    {
        var record = await _feeRepository.Query().FirstOrDefaultAsync(f => f.Id == feeId);
        if (record == null) return false;

        record.Status = FeeStatus.Exempt;
        record.ExemptionType = exemptionType;
        record.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        var student = await _studentRepository.Query().Include(s => s.User).FirstOrDefaultAsync(s => s.Id == record.StudentId);

        // Notify the student (PERSISTED)
        if (student?.UserId > 0)
        {
            await _notificationService.NotifyUserAsync(
                student.UserId,
                "Fee Exempted",
                $"Your fee {record.Description} has been exempted. Type: {exemptionType}",
                "info",
                "/student/fees");
        }

        // Notify the admin (PERSISTED)
        await _notificationService.NotifyUserAsync(
            actorUserId,
            "Fee Exempted",
            $"You exempted fee for {student?.User?.FullName ?? "student"}: {record.Description}",
            "info");

        return true;
    }

    public async Task<FeeRecordDto?> GetByIdAsync(int id)
    {
        return await _feeRepository.Query()
            .Where(f => f.Id == id)
            .Include(f => f.Student).ThenInclude(s => s!.User)
            .Select(f => new FeeRecordDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                StudentName = f.Student!.User!.FullName,
                Amount = f.Amount,
                AmountPaid = f.AmountPaid,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status.ToString(),
                Description = f.Description,
                TransactionId = f.TransactionId,
                ExemptionType = f.ExemptionType
            }).FirstOrDefaultAsync();
    }

    public async Task<decimal> GetStudentBalanceAsync(int studentId)
    {
        var records = await _feeRepository.Query()
            .Where(f => f.StudentId == studentId)
            .ToListAsync();

        return records
            .Where(f => f.Status != FeeStatus.Exempt && f.Status != FeeStatus.Paid)
            .Sum(f => f.Amount - f.AmountPaid);
    }

    public async Task<bool> HasOverdueFeesAsync(int studentId)
    {
        return await _feeRepository.Query().AnyAsync(f =>
            f.StudentId == studentId && f.Status == FeeStatus.Overdue);
    }

    public async Task<PagedResult<FeeRecordDto>> GetPagedAsync(string? search = null, int page = 1, int pageSize = 10)
    {
        var query = _feeRepository.Query()
            .Include(f => f.Student).ThenInclude(s => s!.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var terms = search.Trim().ToLower().Split(' ');
            query = query.Where(f => f.Student != null && f.Student.User != null &&
                terms.All(t => f.Student.User.FullName.ToLower().Contains(t)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeeRecordDto
            {
                Id = f.Id,
                StudentId = f.StudentId,
                StudentName = f.Student != null && f.Student.User != null ? f.Student.User.FullName : null,
                Amount = f.Amount,
                AmountPaid = f.AmountPaid,
                DueDate = f.DueDate,
                PaidDate = f.PaidDate,
                Status = f.Status.ToString(),
                Description = f.Description,
                TransactionId = f.TransactionId,
                ExemptionType = f.ExemptionType
            })
            .ToListAsync();

        return new PagedResult<FeeRecordDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
