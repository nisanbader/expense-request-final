using Expense.Api.Data;
using Expense.Api.Models;
using Expense.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Expense.Api.Controllers;

[ApiController]
[Route("api/requests/{id}/attachments")]
[Authorize(Roles = "Employee")]
public class AttachmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IConfiguration _config;

    public AttachmentsController(AppDbContext db, IFileStorage storage, IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _config = config;
    }

    [HttpPost]
    [RequestSizeLimit(16777216)]
    public async Task<IActionResult> Upload(Guid id, List<IFormFile> files, CancellationToken ct)
    {
        var userId = GetUserId();
        var req = await _db.ExpenseRequests.Include(r => r.Attachments).FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == userId, ct);
        if (req == null) return NotFound();
        if (req.Status != RequestStatus.Draft) return Conflict(new { error = "Only draft can be modified" });

        var allowed = _config.GetSection("Uploads:AllowedContentTypes").Get<string[]>() ?? Array.Empty<string>();
        var maxSize = _config.GetValue<long>("Uploads:MaxFileSizeBytes", 15 * 1024 * 1024);
        var maxCount = _config.GetValue<int>("Uploads:MaxFileCount", 10);

        if (req.Attachments.Count + files.Count > maxCount)
        {
            return BadRequest(new { error = $"Total attachments cannot exceed {maxCount}" });
        }

        foreach (var file in files)
        {
            if (file.Length == 0) continue;
            if (file.Length > maxSize) return BadRequest(new { error = $"File too large: {file.FileName}" });
            if (!allowed.Contains(file.ContentType)) return BadRequest(new { error = $"Invalid content type: {file.ContentType}" });

            await using var stream = file.OpenReadStream();
            var path = await _storage.SaveAsync(stream, file.FileName, file.ContentType, ct);

            req.Attachments.Add(new Attachment
            {
                Id = Guid.NewGuid(),
                ExpenseRequestId = req.Id,
                FileName = file.FileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                StoragePath = path
            });
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { count = req.Attachments.Count });
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        return Guid.Parse(sub!);
    }
}

