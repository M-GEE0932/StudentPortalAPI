namespace StudentPortalAPI.Services;

using Microsoft.EntityFrameworkCore;
using StudentPortalAPI.Repositories;
using StudentPortalAPI.Repositories.Interfaces;
using StudentPortalAPI.DTOs;
using StudentPortalAPI.Models;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IFacultyRepository _facultyRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthenticationService(
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IFacultyRepository facultyRepository,
        IJwtService jwtService,
        IPasswordHasher passwordHasher,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _studentRepository = studentRepository;
        _facultyRepository = facultyRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already registered");

        var role = request.Role.ToLower() == "faculty" ? UserRole.Faculty : UserRole.Student;

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FullName = request.FullName,
            Role = role,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        if (role == UserRole.Student)
        {
            var stuYear = DateTime.UtcNow.Year.ToString().Substring(2);
            var student = new Student
            {
                UserId = user.Id,
                EnrollmentDate = DateTime.UtcNow,
                StudentId = $"STU{stuYear}{(await _studentRepository.GetAllAsync()).Count() + 1:D4}"
            };
            await _studentRepository.AddAsync(student);
        }
        else
        {
            var facYear = DateTime.UtcNow.Year.ToString().Substring(2);
            var faculty = new Faculty
            {
                UserId = user.Id,
                HireDate = DateTime.UtcNow,
                FacultyId = $"FAC{facYear}{(await _facultyRepository.GetAllAsync()).Count() + 1:D4}"
            };
            await _facultyRepository.AddAsync(faculty);
        }

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogAsync(user.Id, "Register", "User", user.Id);

        return new AuthResponse
        {
            Token = _jwtService.GenerateToken(user),
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            UserId = user.Id,
            IsActive = user.IsActive
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserWithRoleDataAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is pending approval. Please wait for admin approval.");

        await _auditService.LogAsync(user.Id, "Login", "User", user.Id);

        return new AuthResponse
        {
            Token = _jwtService.GenerateToken(user),
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            UserId = user.Id,
            IsActive = user.IsActive
        };
    }
}
