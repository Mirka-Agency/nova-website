using CMS.Domain.Common;

namespace CMS.Infrastructure.Audit;

public sealed class AuditEntry : BaseEntity
{
    private AuditEntry()
    {
    }

    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? UserId { get; private set; }
    public string? UserEmail { get; private set; }
    public string? Details { get; private set; }

    public static AuditEntry Create(
        string action,
        string entityType,
        string? entityId,
        string? userId,
        string? userEmail,
        string? details)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Entity type is required.", nameof(entityType));

        return new AuditEntry
        {
            Action = action.Trim(),
            EntityType = entityType.Trim(),
            EntityId = Normalize(entityId, 100),
            UserId = Normalize(userId, 450),
            UserEmail = Normalize(userEmail, 256),
            Details = Normalize(details, 2000)
        };
    }

    private static string? Normalize(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
