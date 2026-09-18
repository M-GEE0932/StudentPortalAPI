using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentPortalAPI.Services;
using StudentPortalAPI.DTOs;


namespace StudentPortalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeesController : ControllerBase
{
    private readonly IFeeService _feeService;

    public FeesController(IFeeService feeService)
    {
        _feeService = feeService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");
        return userId;
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<FeeRecordDto>>> GetByStudent(int studentId)
    {
        return Ok(await _feeService.GetByStudentAsync(studentId));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<FeeRecordDto>>> GetAll()
    {
        return Ok(await _feeService.GetAllAsync());
    }

    /// <summary>
    /// Server-side paginated fee list with optional student-name search.
    /// Multi-word, case-insensitive: "mut gat" finds student "Mut Gatkek".
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<FeeRecordDto>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        return Ok(await _feeService.GetPagedAsync(search, page, pageSize));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("dashboard")]
    public async Task<ActionResult<FeeDashboardDto>> GetDashboard()
    {
        return Ok(await _feeService.GetDashboardAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FeeRecordDto>> Create([FromBody] CreateFeeRecordRequest request)
    {
        var actorUserId = GetCurrentUserId();
        var result = await _feeService.CreateAsync(request, actorUserId);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id}/pay")]
    public async Task<ActionResult<FeeRecordDto>> Pay(int id, [FromBody] PayFeeRequest request)
    {
        var actorUserId = GetCurrentUserId();
        var result = await _feeService.PayAsync(id, request, actorUserId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/exempt")]
    public async Task<ActionResult> Exempt(int id, [FromBody] ExemptRequest request)
    {
        var actorUserId = GetCurrentUserId();
        var success = await _feeService.ExemptAsync(id, request.ExemptionType, actorUserId);
        if (!success) return NotFound();
        return Ok(new { message = "Fee exempted" });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FeeRecordDto>> GetById(int id)
    {
        var fee = await _feeService.GetByIdAsync(id);
        if (fee == null) return NotFound();
        return Ok(fee);
    }

    [HttpGet("balance/{studentId}")]
    public async Task<ActionResult<decimal>> GetBalance(int studentId)
    {
        return Ok(await _feeService.GetStudentBalanceAsync(studentId));
    }
}
