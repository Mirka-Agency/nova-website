using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductSpecification : BaseEntity
{
    private ProductSpecification()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static ProductSpecification Create(Guid productId, string key, string value, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("کلید ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار ویژگی الزامی است.");

        return new ProductSpecification
        {
            ProductId = productId,
            Key = key.Trim(),
            Value = value.Trim(),
            SortOrder = Math.Max(0, sortOrder)
        };
    }

    public void Update(string key, string value, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("کلید ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار ویژگی الزامی است.");

        Key = key.Trim();
        Value = value.Trim();
        SortOrder = Math.Max(0, sortOrder);
        Touch();
    }
}
