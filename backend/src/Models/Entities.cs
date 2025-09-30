using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expense.Api.Models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    [Required]
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool Revoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExpenseRequest
{
    public Guid Id { get; set; }

    [Required]
    public Guid EmployeeId { get; set; }
    public User? Employee { get; set; }

    public DateOnly ExpenseDate { get; set; }

    [Required]
    [MaxLength(512)]
    public string EventDescription { get; set; } = string.Empty;

    public bool IsDomestic { get; set; }

    [MaxLength(8)]
    public string? RegionCode { get; set; } // TR/US/EU/UK/Other

    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    public List<ExpenseItem> Items { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
    public List<RequestAudit> Audits { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExpenseItem
{
    public Guid Id { get; set; }
    public Guid ExpenseRequestId { get; set; }
    public ExpenseRequest? ExpenseRequest { get; set; }

    public ExpenseCategory Category { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(8)]
    public string Currency { get; set; } = "TRY";

    public ItemStatus Status { get; set; } = ItemStatus.Pending;

    [MaxLength(1024)]
    public string? FinanceNote { get; set; }
}

public class Attachment
{
    public Guid Id { get; set; }
    public Guid ExpenseRequestId { get; set; }
    public ExpenseRequest? ExpenseRequest { get; set; }

    [Required]
    [MaxLength(256)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [Required]
    [MaxLength(512)]
    public string StoragePath { get; set; } = string.Empty;
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    [Required]
    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;
    [Required]
    [MaxLength(128)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(1024)]
    public string? Message { get; set; }
    public bool Read { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RequestAudit
{
    public Guid Id { get; set; }
    public Guid ExpenseRequestId { get; set; }
    public ExpenseRequest? ExpenseRequest { get; set; }
    public UserRole ActorRole { get; set; }
    public Guid ActorUserId { get; set; }
    [Required]
    [MaxLength(64)]
    public string Action { get; set; } = string.Empty;
    [MaxLength(1024)]
    public string? Details { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

