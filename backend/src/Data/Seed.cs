using Expense.Api.Models;
using Expense.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Data;

public static class Seed
{
    public static async Task EnsureSeededAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (await db.Users.AnyAsync(ct)) return;

        var employee = new User { Id = Guid.NewGuid(), Username = "employee1", Role = UserRole.Employee, PasswordHash = hasher.Hash("Passw0rd!") };
        var manager = new User { Id = Guid.NewGuid(), Username = "manager1", Role = UserRole.Manager, PasswordHash = hasher.Hash("Passw0rd!") };
        var finance = new User { Id = Guid.NewGuid(), Username = "finance1", Role = UserRole.Finance, PasswordHash = hasher.Hash("Passw0rd!") };

        db.Users.AddRange(employee, manager, finance);
        await db.SaveChangesAsync(ct);
    }
}

