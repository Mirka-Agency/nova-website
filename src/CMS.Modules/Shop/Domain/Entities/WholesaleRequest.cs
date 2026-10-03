using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Domain.Entities;

public class WholesaleRequest : BaseEntity
{
    private WholesaleRequest()
    {
    }

    public string? UserId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string BusinessInfo { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string? DocumentUrlsJson { get; private set; }
    public WholesaleRequestStatus Status { get; private set; } = WholesaleRequestStatus.Pending;
    public string? AdminNotes { get; private set; }
    public Guid? AssignedGroupId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }

    public static WholesaleRequest Create(
        string? userId,
        string companyName,
        string businessInfo,
        string contactName,
        string phone,
        string email,
        string address,
        string? documentUrlsJson)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("نام شرکت الزامی است.");
        if (string.IsNullOrWhiteSpace(contactName))
            throw new DomainException("نام تماس الزامی است.");
        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("تلفن الزامی است.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("ایمیل الزامی است.");

        return new WholesaleRequest
        {
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            CompanyName = companyName.Trim(),
            BusinessInfo = businessInfo?.Trim() ?? string.Empty,
            ContactName = contactName.Trim(),
            Phone = phone.Trim(),
            Email = email.Trim(),
            Address = address?.Trim() ?? string.Empty,
            DocumentUrlsJson = string.IsNullOrWhiteSpace(documentUrlsJson) ? null : documentUrlsJson.Trim(),
            Status = WholesaleRequestStatus.Pending
        };
    }

    public void Approve(Guid groupId, string? adminNotes)
    {
        if (Status != WholesaleRequestStatus.Pending)
            throw new DomainException("فقط درخواست‌های در انتظار قابل تأیید هستند.");

        Status = WholesaleRequestStatus.Approved;
        AssignedGroupId = groupId;
        AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Reject(string? adminNotes)
    {
        if (Status != WholesaleRequestStatus.Pending)
            throw new DomainException("فقط درخواست‌های در انتظار قابل رد هستند.");

        Status = WholesaleRequestStatus.Rejected;
        AdminNotes = string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim();
        ReviewedAtUtc = DateTime.UtcNow;
        Touch();
    }
}
