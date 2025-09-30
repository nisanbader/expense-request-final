using Expense.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();
    public DbSet<ExpenseItem> ExpenseItems => Set<ExpenseItem>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RequestAudit> RequestAudits => Set<RequestAudit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<ExpenseRequest>()
            .HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ExpenseItem>()
            .HasOne(i => i.ExpenseRequest)
            .WithMany(r => r.Items)
            .HasForeignKey(i => i.ExpenseRequestId);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.ExpenseRequest)
            .WithMany(r => r.Attachments)
            .HasForeignKey(a => a.ExpenseRequestId);

        modelBuilder.Entity<RequestAudit>()
            .HasOne(a => a.ExpenseRequest)
            .WithMany(r => r.Audits)
            .HasForeignKey(a => a.ExpenseRequestId);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId);
    }
}

