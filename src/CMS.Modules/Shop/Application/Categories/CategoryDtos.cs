namespace CMS.Modules.Shop.Application.Categories;

public sealed record ShopCategoryListItemDto(
    Guid Id,
    string Name,
    string Slug,
    int ProductCount,
    string? ImageUrl,
    int SortOrder,
    bool IsActive);

public sealed record ShopCategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Content,
    string? ImageUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    int SortOrder,
    bool IsActive);

public sealed record PublicCategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Content,
    string? ImageUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords);

public sealed record SaveShopCategoryCommand(
    string Name,
    string? Slug,
    string? Description,
    string? Content,
    string? ImageUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    int SortOrder,
    bool IsActive = true);
