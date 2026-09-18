using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Models;

namespace StudentPortalAPI.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicYearSemester> AcademicYearSemesters => Set<AcademicYearSemester>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudentCourse> StudentCourses => Set<StudentCourse>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<FeeRecord> FeeRecords => Set<FeeRecord>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<ResultApproval> ResultApprovals => Set<ResultApproval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.HasIndex(s => s.UserId).IsUnique();
        });

        modelBuilder.Entity<Faculty>(e =>
        {
            e.HasIndex(f => f.UserId).IsUnique();
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.HasIndex(d => d.Name).IsUnique();
        });

        modelBuilder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
        });

        modelBuilder.Entity<AcademicYearSemester>(e =>
        {
            e.HasIndex(a => new { a.DepartmentId, a.YearNumber, a.SemesterNumber }).IsUnique();
        });

        modelBuilder.Entity<StudentCourse>(e =>
        {
            e.HasIndex(sc => new { sc.StudentId, sc.CourseId, sc.AcademicYearSemesterId }).IsUnique();
            e.HasIndex(sc => new { sc.StudentId, sc.IsApproved });
        });

        modelBuilder.Entity<Attendance>(e =>
        {
            e.HasIndex(a => new { a.StudentId, a.CourseId, a.Date }).IsUnique();
            e.HasIndex(a => new { a.StudentId, a.CourseId });
        });

        modelBuilder.Entity<AssignmentSubmission>(e =>
        {
            e.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
        });

        modelBuilder.Entity<FeeRecord>(e =>
        {
            e.HasIndex(f => new { f.StudentId, f.Status });
        });

        modelBuilder.Entity<Notice>(e =>
        {
            e.HasIndex(n => n.TargetType);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.Timestamp);
            e.Property(a => a.OldValues).HasColumnType("jsonb");
            e.Property(a => a.NewValues).HasColumnType("jsonb");
        });

        modelBuilder.Entity<StudentCourse>(e =>
        {
            e.Property(sc => sc.MidtermScore).HasColumnType("decimal(5,2)");
            e.Property(sc => sc.FinalScore).HasColumnType("decimal(5,2)");
            e.Property(sc => sc.TotalScore).HasColumnType("decimal(5,2)");
            e.Property(sc => sc.GradePoints).HasColumnType("decimal(3,2)");
        });

        modelBuilder.Entity<FeeRecord>(e =>
        {
            e.Property(f => f.Amount).HasColumnType("decimal(18,2)");
            e.Property(f => f.AmountPaid).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
            // UserId is a plain string, not a foreign key to Users — map it explicitly
            e.Property(n => n.UserId).IsRequired().HasMaxLength(100);

            // Performance indexes for notification queries
            e.HasIndex(n => n.UserId);
            e.HasIndex(n => n.CreatedAt);
            e.HasIndex(n => n.Read);
            e.HasIndex(n => n.IsDeleted);
            e.HasIndex(n => new { n.UserId, n.IsDeleted, n.Read });
            e.HasIndex(n => new { n.UserId, n.IsDeleted, n.CreatedAt });
            e.HasIndex(n => n.NoticeId);

            e.HasOne(n => n.Notice)
                .WithMany()
                .HasForeignKey(n => n.NoticeId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
