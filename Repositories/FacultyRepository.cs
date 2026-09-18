using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Data;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;
using StudentPortalAPI.Repositories.Interfaces;

namespace StudentPortalAPI.Repositories
{
    public class FacultyRepository : GenericRepository<Faculty>, IFacultyRepository
    {
        public FacultyRepository(ApplicationDbContext context) : base(context) { }

        //  THIS – Override to include User, Department, and Courses
        public new async Task<IEnumerable<Faculty>> GetAllAsync()
            => await _dbSet
                .Include(f => f.User)
                .Include(f => f.Department)
                .Include(f => f.Courses)
                .ToListAsync();

        public async Task<Faculty?> GetFacultyWithUserAsync(int facultyId)
            => await _dbSet.Include(f => f.User).FirstOrDefaultAsync(f => f.Id == facultyId);

        public async Task<Faculty?> GetFacultyByUserIdAsync(int userId)
            => await _dbSet.Include(f => f.User).FirstOrDefaultAsync(f => f.UserId == userId);

        public async Task<IEnumerable<Faculty>> GetFacultiesByDepartmentAsync(int departmentId)
            => await _dbSet.Include(f => f.User).Where(f => f.DepartmentId == departmentId).ToListAsync();

        public async Task<(IEnumerable<FacultyDto> Items, int Total)> GetPagedDtosAsync(int page, int pageSize, string? search)
        {
            var query = _dbSet
                .Include(f => f.User)
                .Include(f => f.Department)
                .Include(f => f.Courses)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(f => f.User != null && f.User.FullName.ToLower().Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(f => f.User != null ? f.User.FullName : "")
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FacultyDto
                {
                    Id = f.Id,
                    UserId = f.UserId,
                    FullName = f.User != null ? f.User.FullName : "",
                    Email = f.User != null ? f.User.Email : "",
                    PhotoUrl = f.User != null ? f.User.PhotoUrl : null,
                    DepartmentId = f.DepartmentId,
                    DepartmentName = f.Department != null ? f.Department.Name : null,
                    FacultyIdNumber = f.FacultyId,
                    Title = f.Title,
                    HireDate = f.HireDate,
                    IsActive = f.User != null && f.User.IsActive,
                    CourseCount = f.Courses.Count
                })
                .ToListAsync();

            return (items, total);
        }
    }
}