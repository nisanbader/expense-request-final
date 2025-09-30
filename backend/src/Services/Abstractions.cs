using Expense.Api.Models;

namespace Expense.Api.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
    Task DeleteAsync(string storagePath, CancellationToken ct);
}

public interface INotificationService
{
    Task NotifyUserAsync(Guid userId, string type, string title, string? message, CancellationToken ct);
    Task NotifyRoleAsync(UserRole role, string type, string title, string? message, CancellationToken ct);
}

