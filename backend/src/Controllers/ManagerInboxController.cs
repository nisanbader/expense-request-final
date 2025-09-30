using Expense.Api.Data;
using Expense.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/manager/requests/{id}")]
[Authorize(Roles = "Manager")]
public class ManagerRequestDetailsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ManagerRequestDetailsController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var req = await _db.ExpenseRequests.Include(r => r.Items).Include(r => r.Attachments).Include(r => r.Audits).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (req == null) return NotFound();
        return Ok(req);
    }
}

