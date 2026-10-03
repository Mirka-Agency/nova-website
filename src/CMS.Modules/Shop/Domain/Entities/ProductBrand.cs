using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductBrand : BaseEntity
{
    private ProductBrand()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Content { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static ProductBrand Create(
        string name,
        string slug,
        string? description,
        string? content,
        string? logoUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords)
    {
        var brand = new ProductBrand();
        brand.Apply(name, slug, description, content, logoUrl, metaTitle, metaDescription, seoKeywords, true);
        return brand;
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string? content,
        string? logoUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        bool isActive)
    {
        Apply(name, slug, description, content, logoUrl, metaTitle, metaDescription, seoKeywords, isActive);
        Touch();
    }

    private void Apply(
        string name,
        string slug,
        string? description,
        string? content,
        string? logoUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام برند الزامی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک برند الزامی است.");
        if (content is { Length: > 500_000 })
            throw new DomainException("محتوای برند خیلی طولانی است.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Content = string.IsNullOrWhiteSpace(content) ? null : content;
        LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
        MetaTitle = Truncate(metaTitle, 200);
        MetaDescription = Truncate(metaDescription, 500);
        SeoKeywords = Truncate(seoKeywords, 500);
        IsActive = isActive;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
