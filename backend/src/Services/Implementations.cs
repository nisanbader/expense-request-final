using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Expense.Api.Data;
using Expense.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Expense.Api.Services;

public class JwtSettings
{
    public string Issuer { get; set; } = "ExpenseApp";
    public string Audience { get; set; } = "ExpenseAppClients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IWebHostEnvironment env, ILogger<LocalFileStorage> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
    {
        var safeName = Path.GetFileName(fileName);
        var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsRoot);
        var unique = $"{Guid.NewGuid()}_{safeName}";
        var fullPath = Path.Combine(uploadsRoot, unique);
        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);
        return fullPath;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        try
        {
            if (File.Exists(storagePath)) File.Delete(storagePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {Path}", storagePath);
        }
        return Task.CompletedTask;
    }
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;
    private readonly NotificationHubDispatcher _dispatcher;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger, NotificationHubDispatcher dispatcher)
    {
        _db = db;
        _logger = logger;
        _dispatcher = dispatcher;
    }

    public async Task NotifyUserAsync(Guid userId, string type, string title, string? message, CancellationToken ct)
    {
        var notif = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow,
            Read = false
        };
        _db.Notifications.Add(notif);
        await _db.SaveChangesAsync(ct);
        await _dispatcher.SendToUserAsync(userId, notif, ct);
    }

    public async Task NotifyRoleAsync(UserRole role, string type, string title, string? message, CancellationToken ct)
    {
        var userIds = await _db.Users.Where(u => u.Role == role).Select(u => u.Id).ToListAsync(ct);
        foreach (var id in userIds)
        {
            await NotifyUserAsync(id, type, title, message, ct);
        }
    }
}

