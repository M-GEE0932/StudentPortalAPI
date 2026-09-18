using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-dashboard")]
    public async Task<ActionResult> GetAdminDashboard()
    {
        return Ok(await _reportService.GetAdminDashboardAsync());
    }

    [Authorize(Roles = "Student")]
    [HttpGet("student-dashboard")]
    public async Task<ActionResult> GetStudentDashboard()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var dashboard = await _reportService.GetStudentDashboardAsync(userId);
        if (dashboard == null) return NotFound();
        return Ok(dashboard);
    }

    [Authorize(Roles = "Faculty")]
    [HttpGet("faculty-dashboard")]
    public async Task<ActionResult> GetFacultyDashboard()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var dashboard = await _reportService.GetFacultyDashboardAsync(userId);
        if (dashboard == null) return NotFound();
        return Ok(dashboard);
    }
}
