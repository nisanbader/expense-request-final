using Expense.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty);
        var list = await _db.Notifications.Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync(ct);
        var unread = list.Count(n => !n.Read);
        return Ok(new { items = list, unread });
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name) ?? string.Empty);
        var toUpdate = await _db.Notifications.Where(n => n.UserId == userId && !n.Read).ToListAsync(ct);
        foreach (var n in toUpdate) n.Read = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

