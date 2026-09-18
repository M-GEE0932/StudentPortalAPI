using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Department?> GetDepartmentWithDetailsAsync(int id)
            => await Query()
                .Include(d => d.Students)
                .Include(d => d.Faculties)
                .Include(d => d.Courses)
                .Include(d => d.AcademicYearSemesters)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<IEnumerable<Department>> GetDepartmentsAsync(string? searchTerm = null, int? durationYears = null)
        {
            var query = Query()
                .Include(d => d.Students)
                .Include(d => d.Faculties)
                .Include(d => d.Courses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(term));
            }

            if (durationYears.HasValue)
                query = query.Where(d => d.DurationYears == durationYears.Value);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<DepartmentDto>> GetDepartmentDtosAsync(string? searchTerm = null, int? durationYears = null)
        {
            var depts = await GetDepartmentsAsync(searchTerm, durationYears);
            return depts.Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                DurationYears = d.DurationYears,
                StudentCount = d.Students?.Count ?? 0,
                FacultyCount = d.Faculties?.Count ?? 0,
                CourseCount = d.Courses?.Count ?? 0,
                CreatedAt = d.CreatedAt
            }).ToList();
        }

        public async Task<(IEnumerable<DepartmentDto> Items, int Total)> GetPagedDtosAsync(string? searchTerm, int? durationYears, int page, int pageSize)
        {
            var query = Query()
                .Include(d => d.Students)
                .Include(d => d.Faculties)
                .Include(d => d.Courses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(term));
            }

            if (durationYears.HasValue)
                query = query.Where(d => d.DurationYears == durationYears.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return (items, total);
        }

        public async Task<bool> DepartmentNameExistsAsync(string name, int? excludeId = null)
        {
            var query = Query().Where(d => d.Name == name);
            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}
