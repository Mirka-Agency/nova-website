using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Domain.Entities;

public class Comment : BaseEntity
{
    private Comment()
    {
    }

    public CommentTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public string TargetTitle { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public string? AuthorEmail { get; private set; }
    public string? AuthorPhone { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public CommentStatus Status { get; private set; } = CommentStatus.Pending;
    public DateTime PublishedAtUtc { get; private set; }

    public static Comment Create(
        CommentTargetType targetType,
        Guid targetId,
        string targetTitle,
        string? userId,
        string authorName,
        string? authorEmail,
        string? authorPhone,
        string body)
    {
        if (!Enum.IsDefined(targetType))
            throw new DomainException("نوع هدف کامنت نامعتبر است.");
        if (targetId == Guid.Empty)
            throw new DomainException("شناسه هدف کامنت نامعتبر است.");
        if (string.IsNullOrWhiteSpace(targetTitle))
            throw new DomainException("عنوان هدف کامنت الزامی است.");
        if (string.IsNullOrWhiteSpace(authorName))
            throw new DomainException("نام الزامی است.");
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("متن کامنت الزامی است.");

        var now = DateTime.UtcNow;
        return new Comment
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetTitle = targetTitle.Trim(),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            AuthorName = authorName.Trim(),
            AuthorEmail = NormalizeOptional(authorEmail, 256),
            AuthorPhone = NormalizeOptional(authorPhone, 40),
            Body = body.Trim(),
            Status = CommentStatus.Pending,
            PublishedAtUtc = now
        };
    }

    public void Approve()
    {
        Status = CommentStatus.Approved;
        Touch();
    }

    public void Reject()
    {
        Status = CommentStatus.Rejected;
        Touch();
    }

    public void UpdateContent(string body, DateTime publishedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("متن کامنت الزامی است.");
        if (publishedAtUtc == default)
            throw new DomainException("تاریخ انتشار نامعتبر است.");

        Body = body.Trim();
        PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc, DateTimeKind.Utc);
        Touch();
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"مقدار نباید بیشتر از {maxLength} نویسه باشد.");
        return trimmed;
    }
}
