using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IFeeRepository _feeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ProfileService(
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IStudentCourseRepository studentCourseRepository,
        IAttendanceRepository attendanceRepository,
        IFacultyRepository facultyRepository,
        IFeeRepository feeRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _studentCourseRepository = studentCourseRepository;
        _attendanceRepository = attendanceRepository;
        _facultyRepository = facultyRepository;
        _feeRepository = feeRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<ProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var profile = new ProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhotoUrl = user.PhotoUrl,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };

        if (user.Role == UserRole.Student)
        {
            var student = await _studentRepository.Query()
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student != null)
            {
                var approvedCourses = await _studentCourseRepository.Query()
                    .Include(sc => sc.Course)
                    .Where(sc => sc.StudentId == student.Id && sc.IsApproved && sc.TotalScore.HasValue)
                    .ToListAsync();

                var totalCredits = approvedCourses.Sum(sc => sc.Course?.Credits ?? 0);
                var totalGradePoints = approvedCourses.Sum(sc => (sc.GradePoints ?? 0) * (sc.Course?.Credits ?? 0));
                var cgpa = totalCredits > 0 ? totalGradePoints / totalCredits : 0;

                var enrolledCourses = await _studentCourseRepository.Query()
                    .CountAsync(sc => sc.StudentId == student.Id);

                var attendances = await _attendanceRepository.Query()
                    .Where(a => a.StudentId == student.Id)
                    .ToListAsync();
                var totalClasses = attendances.Count;
                var presentClasses = attendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.Excused);
                var attendancePct = totalClasses > 0 ? (decimal)presentClasses / totalClasses * 100 : 100;

                var pendingResults = await _studentCourseRepository.Query()
                    .CountAsync(sc => sc.StudentId == student.Id && !sc.IsApproved && sc.TotalScore.HasValue);

                var fees = await _feeRepository.Query()
                    .Where(f => f.StudentId == student.Id)
                    .ToListAsync();
                var totalFees = fees.Sum(f => f.Amount);
                var paidFees = fees.Where(f => f.Status == FeeStatus.Paid).Sum(f => f.AmountPaid);
                var overdueFees = fees.Where(f => f.Status == FeeStatus.Overdue).Sum(f => f.Amount - f.AmountPaid);
                var feeStatus = fees.Any(f => f.Status == FeeStatus.Overdue) ? "Overdue" :
                                fees.Any(f => f.Status == FeeStatus.Pending || f.Status == FeeStatus.PartiallyPaid) ? "Pending" :
                                fees.Count > 0 && fees.All(f => f.Status == FeeStatus.Paid || f.Status == FeeStatus.Exempt) ? "Paid" : "No Fees";

                profile.StudentProfile = new StudentProfileDto
                {
                    StudentIdNumber = student.StudentId,
                    DepartmentName = student.Department?.Name,
                    CGPA = Math.Round(cgpa, 2),
                    TotalCreditsEarned = totalCredits,
                    CurrentYear = student.CurrentYear,
                    CurrentSemester = student.CurrentSemester,
                    EnrollmentDate = student.EnrollmentDate,
                    IsGraduated = student.IsGraduated,
                    EnrolledCourses = enrolledCourses,
                    ApprovedCourses = approvedCourses.Count,
                    AttendancePercentage = Math.Round(attendancePct, 1),
                    PendingResults = pendingResults,
                    TotalFees = totalFees,
                    PaidFees = paidFees,
                    OverdueFees = overdueFees,
                    FeeStatus = feeStatus
                };
            }
        }
        else if (user.Role == UserRole.Faculty)
        {
            var faculty = await _facultyRepository.Query()
                .Include(f => f.Department)
                .Include(f => f.Courses)
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (faculty != null)
            {
                profile.FacultyProfile = new FacultyProfileDto
                {
                    FacultyIdNumber = faculty.FacultyId,
                    DepartmentName = faculty.Department?.Name,
                    Title = faculty.Title,
                    HireDate = faculty.HireDate,
                    CourseCount = faculty.Courses.Count
                };
            }
        }

        return profile;
    }

    public async Task<ProfileDto?> UpdateProfileAsync(int userId, UpdateProfileRequest request, int actorUserId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var oldName = user.FullName;

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.PhotoUrl != null) user.PhotoUrl = request.PhotoUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        // Notify the subject (PERSISTED)
        await _notificationService.NotifyUserAsync(
            userId,
            "Profile Updated",
            "Your profile has been updated successfully.",
            "success");

        // Notify the actor if different (PERSISTED)
        if (actorUserId != userId)
        {
            var subjectDisplayName = request.FullName ?? oldName;
            await _notificationService.NotifyUserAsync(
                actorUserId,
                "Profile Updated",
                $"You updated the profile of {subjectDisplayName}.",
                "info");
        }

        return await GetProfileAsync(userId);
    }
}
