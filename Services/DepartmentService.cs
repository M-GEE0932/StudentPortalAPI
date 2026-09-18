using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.Services;

namespace StudentPortalAPI.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAcademicYearSemesterRepository _academicYearSemesterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IAcademicYearSemesterRepository academicYearSemesterRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService)
    {
        _departmentRepository = departmentRepository;
        _academicYearSemesterRepository = academicYearSemesterRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
    }

    public async Task<List<DepartmentDto>> GetAllAsync(string? searchTerm = null, int? durationYears = null)
    {
        var query = _departmentRepository.Query().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(term));
        }

        if (durationYears.HasValue)
        {
            query = query.Where(d => d.DurationYears == durationYears.Value);
        }

        return await query
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                DurationYears = d.DurationYears,
                StudentCount = d.Students.Count,
                FacultyCount = d.Faculties.Count,
                CourseCount = d.Courses.Count,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var d = await _departmentRepository.Query()
            .Include(x => x.Students)
            .Include(x => x.Faculties)
            .Include(x => x.Courses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (d == null) return null;

        return new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            DurationYears = d.DurationYears,
            StudentCount = d.Students.Count,
            FacultyCount = d.Faculties.Count,
            CourseCount = d.Courses.Count,
            CreatedAt = d.CreatedAt
        };
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request)
    {
        if (await _departmentRepository.Query().AnyAsync(d => d.Name == request.Name))
            throw new InvalidOperationException("Department name already exists");

        var dept = new Department
        {
            Name = request.Name,
            Description = request.Description,
            DurationYears = request.DurationYears,
            CreatedAt = DateTime.UtcNow
        };

        _departmentRepository.Add(dept);
        await _unitOfWork.SaveChangesAsync();

        var currentYear = DateTime.UtcNow.Year;
        var academicYear = $"{currentYear}/{currentYear + 1}";
        for (int year = 1; year <= dept.DurationYears; year++)
        {
            for (int sem = 1; sem <= 2; sem++)
            {
                _academicYearSemesterRepository.Add(new AcademicYearSemester
                {
                    DepartmentId = dept.Id,
                    YearNumber = year,
                    SemesterNumber = sem,
                    AcademicYear = academicYear,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(null, "Create", "Department", dept.Id, newValues: new { dept.Name, dept.DurationYears });

        return new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Description = dept.Description,
            DurationYears = dept.DurationYears,
            StudentCount = 0,
            FacultyCount = 0,
            CourseCount = 0,
            CreatedAt = dept.CreatedAt
        };
    }

    public async Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentRequest request)
    {
        var dept = await _departmentRepository.GetByIdAsync(id);
        if (dept == null) return null;

        if (request.Name != null) dept.Name = request.Name;
        if (request.Description != null) dept.Description = request.Description;
        if (request.DurationYears.HasValue) dept.DurationYears = request.DurationYears.Value;

        dept.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var dept = await _departmentRepository.Query()
            .Include(d => d.Students)
            .Include(d => d.Courses)
            .Include(d => d.Faculties)
            .Include(d => d.AcademicYearSemesters)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept == null) return false;

        var errors = new List<string>();
        if (dept.Students.Any())
            errors.Add($"{dept.Students.Count} student(s)");
        if (dept.Courses.Any())
            errors.Add($"{dept.Courses.Count} course(s)");
        if (dept.Faculties.Any())
            errors.Add($"{dept.Faculties.Count} faculty member(s)");
        if (dept.AcademicYearSemesters.Any())
            errors.Add($"{dept.AcademicYearSemesters.Count} year-semester record(s)");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Cannot delete department '{dept.Name}'. It has: {string.Join(", ", errors)}. Please remove or reassign these dependencies first.");
        }

        _departmentRepository.Remove(dept);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(null, "Delete", "Department", id, oldValues: new { dept.Name });

        return true;
    }

    public async Task<List<AcademicYearSemesterDto>> GetYearSemestersAsync(int departmentId)
    {
        return await _academicYearSemesterRepository.Query()
            .Where(a => a.DepartmentId == departmentId)
            .OrderBy(a => a.YearNumber).ThenBy(a => a.SemesterNumber)
            .Select(a => new AcademicYearSemesterDto
            {
                Id = a.Id,
                DepartmentId = a.DepartmentId,
                YearNumber = a.YearNumber,
                SemesterNumber = a.SemesterNumber,
                AcademicYear = a.AcademicYear,
                IsActive = a.IsActive
            }).ToListAsync();
    }

    public async Task<List<AcademicYearSemesterDto>> GenerateYearSemestersAsync(int departmentId, string academicYear)
    {
        var dept = await _departmentRepository.GetByIdAsync(departmentId)
            ?? throw new KeyNotFoundException("Department not found");

        var existing = await _academicYearSemesterRepository.Query()
            .Where(a => a.DepartmentId == departmentId && a.AcademicYear == academicYear).ToListAsync();

        if (existing.Any())
            throw new InvalidOperationException("Year semesters already exist for this academic year");

        var result = new List<AcademicYearSemesterDto>();

        for (int year = 1; year <= dept.DurationYears; year++)
        {
            for (int sem = 1; sem <= 2; sem++)
            {
                var ays = new AcademicYearSemester
                {
                    DepartmentId = departmentId,
                    YearNumber = year,
                    SemesterNumber = sem,
                    AcademicYear = academicYear,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _academicYearSemesterRepository.Add(ays);

                result.Add(new AcademicYearSemesterDto
                {
                    DepartmentId = departmentId,
                    YearNumber = year,
                    SemesterNumber = sem,
                    AcademicYear = academicYear,
                    IsActive = true
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return result;
    }

    // NEW: Delete a single year-semester record
    public async Task<bool> DeleteYearSemesterAsync(int id)
    {
        var ys = await _academicYearSemesterRepository.GetByIdAsync(id);
        if (ys == null) return false;

        _academicYearSemesterRepository.Remove(ys);
        await _unitOfWork.SaveChangesAsync();

        // Optional: audit log
        await _auditService.LogAsync(null, "Delete", "AcademicYearSemester", id, oldValues: new { ys.YearNumber, ys.SemesterNumber, ys.AcademicYear });

        return true;
    }

    public async Task<PagedResult<DepartmentDto>> GetPagedAsync(string? searchTerm = null, int? durationYears = null, int page = 1, int pageSize = 10)
    {
        var query = _departmentRepository.Query()
            .Include(d => d.Students).Include(d => d.Faculties).Include(d => d.Courses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(term));
        }
        if (durationYears.HasValue)
            query = query.Where(d => d.DurationYears == durationYears.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(d => d.Name).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new DepartmentDto
            {
                Id = d.Id, Name = d.Name, Description = d.Description, DurationYears = d.DurationYears,
                StudentCount = d.Students.Count, FacultyCount = d.Faculties.Count, CourseCount = d.Courses.Count, CreatedAt = d.CreatedAt
            }).ToListAsync();

        return new PagedResult<DepartmentDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }
}
