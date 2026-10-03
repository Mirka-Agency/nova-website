using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ShopCategory : BaseEntity
{
    private readonly List<Product> _products = [];

    private ShopCategory()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Content { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }
    public string? SeoKeywords { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<Product> Products => _products;

    public static ShopCategory Create(
        string name,
        string slug,
        string? description,
        string? content,
        string? imageUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        int sortOrder,
        bool isActive = true)
    {
        var category = new ShopCategory();
        category.Apply(name, slug, description, content, imageUrl, metaTitle, metaDescription, seoKeywords, sortOrder, isActive);
        return category;
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string? content,
        string? imageUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        int sortOrder,
        bool isActive)
    {
        Apply(name, slug, description, content, imageUrl, metaTitle, metaDescription, seoKeywords, sortOrder, isActive);
        Touch();
    }

    private void Apply(
        string name,
        string slug,
        string? description,
        string? content,
        string? imageUrl,
        string? metaTitle,
        string? metaDescription,
        string? seoKeywords,
        int sortOrder,
        bool isActive)
    {
        Validate(name, slug);
        if (content is { Length: > 500_000 })
            throw new DomainException("محتوای دسته خیلی طولانی است.");

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Content = string.IsNullOrWhiteSpace(content) ? null : content;
        ImageUrl = NormalizeUrl(imageUrl);
        MetaTitle = Truncate(metaTitle, 200);
        MetaDescription = Truncate(metaDescription, 500);
        SeoKeywords = Truncate(seoKeywords, 500);
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        IsActive = isActive;
    }

    private static string? NormalizeUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static void Validate(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام دسته الزامی است.");
        if (name.Trim().Length > 200)
            throw new DomainException("نام دسته خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("نامک دسته الزامی است.");
        if (slug.Trim().Length > 200)
            throw new DomainException("نامک دسته خیلی طولانی است.");
    }
}
