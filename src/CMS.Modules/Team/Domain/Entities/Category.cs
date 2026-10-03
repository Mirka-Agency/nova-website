using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Team.Domain.Entities;

public class Category : BaseEntity
{
    private readonly List<TeamItem> _teamItems = [];

    private Category()
    {
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }
    public IReadOnlyCollection<TeamItem> TeamItems => _teamItems;

    public static Category Create(string name, string slug, string? description = null, string? imageUrl = null)
    {
        Validate(name, slug);
        return new Category
        {
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description ?? string.Empty,
            ImageUrl = NormalizeImageUrl(imageUrl)
        };
    }

    public void Update(string name, string slug, string? description = null, string? imageUrl = null)
    {
        Validate(name, slug);
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description ?? string.Empty;
        ImageUrl = NormalizeImageUrl(imageUrl);
        Touch();
    }

    private static string? NormalizeImageUrl(string? imageUrl) =>
        string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

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
