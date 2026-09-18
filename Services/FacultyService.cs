using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class FacultyService : IFacultyService
{
    private readonly IFacultyRepository _facultyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IStudentCourseRepository _studentCourseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public FacultyService(
        IFacultyRepository facultyRepository,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IStudentCourseRepository studentCourseRepository,
        IUnitOfWork unitOfWork)
    {
        _facultyRepository = facultyRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _studentCourseRepository = studentCourseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<FacultyDto>> GetAllPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _facultyRepository.Query()
            .Include(f => f.User)
            .Include(f => f.Department)
            .Include(f => f.Courses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(f => f.User != null && f.User.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.User!.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FacultyDto
            {
                Id = f.Id,
                UserId = f.UserId,
                FullName = f.User!.FullName,
                Email = f.User.Email,
                PhotoUrl = f.User.PhotoUrl,
                DepartmentId = f.DepartmentId,
                DepartmentName = f.Department != null ? f.Department.Name : null,
                FacultyIdNumber = f.FacultyId,
                Title = f.Title,
                HireDate = f.HireDate,
                IsActive = f.User.IsActive,
                CourseCount = f.Courses.Count
            })
            .ToListAsync();

        return new PagedResult<FacultyDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<FacultyDto>> GetAllAsync()
    {
        return await _facultyRepository.Query()
            .Include(f => f.User)
            .Include(f => f.Department)
            .Include(f => f.Courses)
            .Select(f => new FacultyDto
            {
                Id = f.Id,
                UserId = f.UserId,
                FullName = f.User!.FullName,
                Email = f.User.Email,
                PhotoUrl = f.User.PhotoUrl,
                DepartmentId = f.DepartmentId,
                DepartmentName = f.Department != null ? f.Department.Name : null,
                FacultyIdNumber = f.FacultyId,
                Title = f.Title,
                HireDate = f.HireDate,
                IsActive = f.User.IsActive,
                CourseCount = f.Courses.Count
            }).ToListAsync();
    }

    public async Task<FacultyDto?> GetByIdAsync(int id)
    {
        var f = await _facultyRepository.Query()
            .Include(fa => fa.User)
            .Include(fa => fa.Department)
            .Include(fa => fa.Courses)
            .FirstOrDefaultAsync(fa => fa.Id == id);

        if (f == null) return null;

        return new FacultyDto
        {
            Id = f.Id,
            UserId = f.UserId,
            FullName = f.User!.FullName,
            Email = f.User.Email,
            PhotoUrl = f.User.PhotoUrl,
            DepartmentId = f.DepartmentId,
            DepartmentName = f.Department?.Name,
            FacultyIdNumber = f.FacultyId,
            Title = f.Title,
            HireDate = f.HireDate,
            IsActive = f.User.IsActive,
            CourseCount = f.Courses.Count
        };
    }

    public async Task<FacultyDto?> GetByUserIdAsync(int userId)
    {
        var f = await _facultyRepository.Query()
            .Include(fa => fa.User)
            .Include(fa => fa.Department)
            .Include(fa => fa.Courses)
            .FirstOrDefaultAsync(fa => fa.UserId == userId);

        if (f == null) return null;

        return new FacultyDto
        {
            Id = f.Id,
            UserId = f.UserId,
            FullName = f.User!.FullName,
            Email = f.User.Email,
            PhotoUrl = f.User.PhotoUrl,
            DepartmentId = f.DepartmentId,
            DepartmentName = f.Department?.Name,
            FacultyIdNumber = f.FacultyId,
            Title = f.Title,
            HireDate = f.HireDate,
            IsActive = f.User.IsActive,
            CourseCount = f.Courses.Count
        };
    }

    public async Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto dto)
    {
        if (await _userRepository.Query().AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already registered");

        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
            ?? throw new KeyNotFoundException("Department not found");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password ?? "Default@123"),
            FullName = dto.FullName,
            Role = UserRole.Faculty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync();

        var facultyId = await GenerateFacultyId();

        var faculty = new Faculty
        {
            UserId = user.Id,
            DepartmentId = dto.DepartmentId,
            FacultyId = facultyId,
            Title = dto.Title,
            HireDate = DateTime.UtcNow,
            IsActive = true
        };

        _facultyRepository.Add(faculty);
        await _unitOfWork.SaveChangesAsync();

        return (await GetByIdAsync(faculty.Id))!;
    }

    public async Task<FacultyDto?> UpdateFacultyAsync(int id, UpdateFacultyDto dto)
    {
        var faculty = await _facultyRepository.Query()
            .Include(f => f.User)
            .Include(f => f.Department)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (faculty == null) return null;

        if (dto.DepartmentId.HasValue)
        {
            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId.Value)
                ?? throw new KeyNotFoundException("Department not found");
            faculty.DepartmentId = dto.DepartmentId.Value;
        }

        if (dto.Title != null)
            faculty.Title = dto.Title;

        if (dto.IsActive.HasValue)
            faculty.User!.IsActive = dto.IsActive.Value;

        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteFacultyAsync(int id)
    {
        var faculty = await _facultyRepository.GetByIdAsync(id);
        if (faculty == null) return false;

        var user = await _userRepository.GetByIdAsync(faculty.UserId);
        if (user != null)
            _userRepository.Remove(user);

        _facultyRepository.Remove(faculty);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<FacultyDto> AssignDepartmentAsync(AssignFacultyRequest request)
    {
        var faculty = await _facultyRepository.Query()
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == request.FacultyId)
            ?? throw new KeyNotFoundException("Faculty not found");

        faculty.DepartmentId = request.DepartmentId;

        if (request.Title != null)
            faculty.Title = request.Title;

        await _unitOfWork.SaveChangesAsync();
        return (await GetByIdAsync(faculty.Id))!;
    }

    public async Task<List<StudentDto>> GetStudentsByCourseAsync(int courseId)
    {
        return await _studentCourseRepository.Query()
            .Where(sc => sc.CourseId == courseId)
            .Include(sc => sc.Student).ThenInclude(s => s!.User)
            .Include(sc => sc.Student).ThenInclude(s => s!.Department)
            .Select(sc => new StudentDto
            {
                Id = sc.Student!.Id,
                UserId = sc.Student.UserId,
                FullName = sc.Student.User!.FullName,
                Email = sc.Student.User.Email,
                DepartmentId = sc.Student.DepartmentId,
                DepartmentName = sc.Student.Department != null ? sc.Student.Department.Name : null,
                StudentIdNumber = sc.Student.StudentId,
                CGPA = sc.Student.CGPA,
                IsActive = sc.Student.User.IsActive
            }).ToListAsync();
    }

    private async Task<string> GenerateFacultyId()
    {
        var year = DateTime.UtcNow.Year.ToString().Substring(2);
        var count = await _facultyRepository.CountAsync() + 1;
        return $"FAC{year}{count:D4}";
    }
}
