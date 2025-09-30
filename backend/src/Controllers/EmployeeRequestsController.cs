using Expense.Api.Data;
using Expense.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Employee")]
public class RequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    public RequestsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDraft(CancellationToken ct)
    {
        var userId = GetUserId();
        var req = new ExpenseRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId,
            Status = RequestStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ExpenseRequests.Add(req);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = req.Id }, new { id = req.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDraft(Guid id, [FromBody] ExpenseRequest update, CancellationToken ct)
    {
        var userId = GetUserId();
        var req = await _db.ExpenseRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == userId, ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Draft) return Conflict(new { error = "Only draft can be updated" });

        req.ExpenseDate = update.ExpenseDate;
        req.EventDescription = update.EventDescription;
        req.IsDomestic = update.IsDomestic;
        req.RegionCode = update.RegionCode;
        // Replace items (simple approach)
        _db.ExpenseItems.RemoveRange(req.Items);
        req.Items = update.Items.Select(i => new ExpenseItem
        {
            Id = Guid.NewGuid(),
            ExpenseRequestId = req.Id,
            Category = i.Category,
            Amount = i.Amount,
            Currency = update.IsDomestic ? "TRY" : i.Currency,
            Status = ItemStatus.Pending
        }).ToList();

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var req = await _db.ExpenseRequests.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == userId, ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Draft) return Conflict(new { error = "Only draft can be submitted" });
        if (!req.Items.Any()) return BadRequest(new { error = "At least one item is required" });

        foreach (var item in req.Items)
        {
            item.Status = ItemStatus.Pending;
            if (req.IsDomestic) item.Currency = "TRY";
        }
        req.Status = RequestStatus.Submitted;
        _db.RequestAudits.Add(new RequestAudit
        {
            Id = Guid.NewGuid(), ExpenseRequestId = req.Id, ActorRole = UserRole.Employee, ActorUserId = userId, Action = "Submit", CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var userId = GetUserId();
        var list = await _db.ExpenseRequests.Where(r => r.EmployeeId == userId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new { r.Id, r.ExpenseDate, r.EventDescription, r.Status })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var req = await _db.ExpenseRequests.Include(r => r.Items).Include(r => r.Attachments).Include(r => r.Audits)
            .FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == userId, ct);
        if (req == null) return NotFound();
        return Ok(req);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return Guid.Parse(sub!);
    }
}

