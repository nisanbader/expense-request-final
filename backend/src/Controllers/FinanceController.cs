using Expense.Api.Data;
using Expense.Api.Models;
using Expense.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/finance")]
[Authorize(Roles = "Finance")]
public class FinanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifier;

    public FinanceController(AppDbContext db, INotificationService notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    [HttpGet("requests")]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var statuses = new[] { RequestStatus.ManagerApproved, RequestStatus.FinanceProcessing };
        var list = await _db.ExpenseRequests.Where(r => statuses.Contains(r.Status))
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new { r.Id, r.EmployeeId, r.ExpenseDate, r.EventDescription, r.Status })
            .ToListAsync(ct);
        return Ok(list);
    }

    public class ItemUpdateDto { public ItemStatus Status { get; set; } public string? Note { get; set; } }

    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] ItemUpdateDto dto, CancellationToken ct)
    {
        var item = await _db.ExpenseItems.Include(i => i.ExpenseRequest).FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item == null || item.ExpenseRequest == null) return NotFound();
        if (item.Status == ItemStatus.Paid) return Conflict(new { error = "Already paid" });

        item.Status = dto.Status;
        item.FinanceNote = dto.Note;
        var req = item.ExpenseRequest;

        // Header recalculation
        var items = await _db.ExpenseItems.Where(i => i.ExpenseRequestId == req.Id).ToListAsync(ct);
        if (items.All(i => i.Status == ItemStatus.Paid)) req.Status = RequestStatus.Paid;
        else if (items.All(i => i.Status == ItemStatus.Approved)) req.Status = RequestStatus.FullyApproved;
        else if (items.Any(i => i.Status == ItemStatus.Approved)) req.Status = RequestStatus.PartiallyApproved;
        else req.Status = RequestStatus.FinanceProcessing;

        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = req.Id, ActorRole = UserRole.Finance, ActorUserId = Guid.Empty, Action = $"Item {item.Id} -> {dto.Status}", Details = dto.Note, CreatedAtUtc = DateTime.UtcNow });

        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(req.EmployeeId, "FinanceItemUpdate", "Finance updated an item", dto.Note, ct);
        return NoContent();
    }

    [HttpPost("requests/{id}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        var req = await _db.ExpenseRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (req == null) return NotFound();
        if (req.Items.Any(i => i.Status != ItemStatus.Paid)) return Conflict(new { error = "All items must be Paid" });
        req.Status = RequestStatus.Paid;
        _db.RequestAudits.Add(new RequestAudit { Id = Guid.NewGuid(), ExpenseRequestId = id, ActorRole = UserRole.Finance, ActorUserId = Guid.Empty, Action = "MarkPaid", CreatedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        await _notifier.NotifyUserAsync(req.EmployeeId, "Paid", "Your request is fully paid", null, ct);
        return NoContent();
    }
}

