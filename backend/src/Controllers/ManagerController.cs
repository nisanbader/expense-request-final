using Expense.Api.Data;
using Expense.Api.Models;
using Expense.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Roles = "Manager")]
public class ManagerController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifier;

    public ManagerController(AppDbContext db, INotificationService notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetSubmitted([FromQuery] string? status, CancellationToken ct)
    {
        var desired = RequestStatus.Submitted;
        var list = await _db.ExpenseRequests.Where(r => r.Status == desired)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new { r.Id, r.EmployeeId, r.ExpenseDate, r.EventDescription, r.Status })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost("requests/{id}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var req = await _db.ExpenseRequests.FindAsync([id], ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Submitted) return Conflict(new { error = "Only submitted can be approved" });
        req.Status = RequestStatus.ManagerApproved;
        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = id, ActorRole = UserRole.Manager, ActorUserId = Guid.Empty, Action = "Approve", CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(req.EmployeeId, "ManagerApproved", "Request approved by Manager", null, ct);
        await _notifier.NotifyRoleAsync(UserRole.Finance, "ManagerApproved", "New approved request", null, ct);
        return NoContent();
    }

    [HttpPost("requests/{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] string reason, CancellationToken ct)
    {
        var req = await _db.ExpenseRequests.FindAsync([id], ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Submitted) return Conflict(new { error = "Only submitted can be rejected" });
        req.Status = RequestStatus.Rejected;
        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = id, ActorRole = UserRole.Manager, ActorUserId = Guid.Empty, Action = "Reject", Details = reason, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(req.EmployeeId, "Rejected", "Request rejected by Manager", reason, ct);
        return NoContent();
    }

    [HttpPost("requests/{id}/revise")]
    public async Task<IActionResult> Revise(Guid id, [FromBody] string note, CancellationToken ct)
    {
        var req = await _db.ExpenseRequests.FindAsync([id], ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Submitted) return Conflict(new { error = "Only submitted can be revised" });
        req.Status = RequestStatus.Draft;
        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = id, ActorRole = UserRole.Manager, ActorUserId = Guid.Empty, Action = "Revise", Details = note, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(req.EmployeeId, "Revise", "Manager requested revision", note, ct);
        return NoContent();
    }

    public class ItemCurrencyChangeDto { public string Currency { get; set; } = ""; public string? Reason { get; set; } }

    [HttpPut("items/{itemId}/currency")]
    public async Task<IActionResult> ChangeItemCurrency(Guid itemId, [FromBody] ItemCurrencyChangeDto dto, CancellationToken ct)
    {
        var item = await _db.ExpenseItems.Include(i => i.ExpenseRequest).FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item == null) return NotFound();
        if (item.ExpenseRequest == null) return NotFound();
        if (item.ExpenseRequest.Status != RequestStatus.Submitted) return Conflict(new { error = "Can only change currency while Submitted" });
        item.Currency = dto.Currency;
        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = item.ExpenseRequestId, ActorRole = UserRole.Manager, ActorUserId = Guid.Empty, Action = "ItemCurrencyChanged", Details = dto.Reason, CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(item.ExpenseRequest.EmployeeId, "ItemCurrencyChanged", "Manager changed item currency", dto.Reason, ct);
        return NoContent();
    }
}

