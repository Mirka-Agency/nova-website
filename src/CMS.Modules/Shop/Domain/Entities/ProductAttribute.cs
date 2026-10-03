using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductAttribute : BaseEntity
{
    private readonly List<ProductAttributeValue> _values = [];

    private ProductAttribute()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public IReadOnlyCollection<ProductAttributeValue> Values => _values;

    public static ProductAttribute Create(string name, string slug, int sortOrder = 0)
    {
        var attr = new ProductAttribute();
        attr.Apply(name, slug, sortOrder);
        return attr;
    }

    public void Update(string name, string slug, int sortOrder)
    {
        Apply(name, slug, sortOrder);
        Touch();
    }

    public ProductAttributeValue AddValue(string value, string slug, int sortOrder = 0)
    {
        var item = ProductAttributeValue.Create(Id, value, slug, sortOrder);
        _values.Add(item);
        Touch();
        return item;
    }

    private void Apply(string name, string slug, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک ویژگی الزامی است.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        SortOrder = Math.Max(0, sortOrder);
    }
}

public class ProductAttributeValue : BaseEntity
{
    private ProductAttributeValue()
    {
    }

    public Guid AttributeId { get; private set; }
    public ProductAttribute Attribute { get; private set; } = null!;
    public string Value { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }

    public static ProductAttributeValue Create(Guid attributeId, string value, string slug, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک مقدار الزامی است.");

        return new ProductAttributeValue
        {
            AttributeId = attributeId,
            Value = value.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            SortOrder = Math.Max(0, sortOrder)
        };
    }

    public void Update(string value, string slug, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار ویژگی الزامی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک مقدار الزامی است.");

        Value = value.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        SortOrder = Math.Max(0, sortOrder);
        Touch();
    }
}
