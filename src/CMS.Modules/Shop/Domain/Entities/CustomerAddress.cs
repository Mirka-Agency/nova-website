using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class CustomerAddress : BaseEntity
{
    private CustomerAddress()
    {
    }

    public string UserId { get; private set; } = string.Empty;
    public string RecipientName { get; private set; } = string.Empty;
    public string RecipientPhone { get; private set; } = string.Empty;
    public string Province { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string AddressLine { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public static CustomerAddress Create(
        string userId,
        string recipientName,
        string recipientPhone,
        string province,
        string city,
        string? postalCode,
        string addressLine,
        bool isDefault)
    {
        var address = new CustomerAddress();
        address.Apply(userId, recipientName, recipientPhone, province, city, postalCode, addressLine, isDefault);
        return address;
    }

    public void Update(
        string recipientName,
        string recipientPhone,
        string province,
        string city,
        string? postalCode,
        string addressLine,
        bool isDefault)
    {
        Apply(UserId, recipientName, recipientPhone, province, city, postalCode, addressLine, isDefault);
        Touch();
    }

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }

    private void Apply(
        string userId,
        string recipientName,
        string recipientPhone,
        string province,
        string city,
        string? postalCode,
        string addressLine,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("کاربر آدرس نامعتبر است.");
        if (string.IsNullOrWhiteSpace(recipientName))
            throw new DomainException("نام تحویل‌گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(recipientPhone))
            throw new DomainException("موبایل تحویل‌گیرنده الزامی است.");
        if (string.IsNullOrWhiteSpace(province))
            throw new DomainException("استان الزامی است.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("شهر الزامی است.");
        if (string.IsNullOrWhiteSpace(addressLine))
            throw new DomainException("آدرس الزامی است.");

        UserId = userId.Trim();
        RecipientName = recipientName.Trim();
        RecipientPhone = recipientPhone.Trim();
        Province = province.Trim();
        City = city.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        AddressLine = addressLine.Trim();
        IsDefault = isDefault;
    }
}
