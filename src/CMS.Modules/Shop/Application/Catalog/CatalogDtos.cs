using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Catalog;

public sealed record BrandListItemDto(Guid Id, string Name, string Slug, bool IsActive, string? LogoUrl);
public sealed record BrandDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Content,
    string? LogoUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    bool IsActive);
public sealed record SaveBrandCommand(
    string Name,
    string? Slug,
    string? Description,
    string? Content,
    string? LogoUrl,
    string? MetaTitle,
    string? MetaDescription,
    string? SeoKeywords,
    bool IsActive = true);

public sealed record AttributeValueDto(Guid Id, string Value, string Slug, int SortOrder);
public sealed record AttributeListItemDto(Guid Id, string Name, string Slug, int SortOrder, IReadOnlyList<AttributeValueDto> Values);
public sealed record SaveAttributeCommand(string Name, string? Slug, int SortOrder, IReadOnlyList<SaveAttributeValueCommand>? Values);
public sealed record SaveAttributeValueCommand(Guid? Id, string Value, string? Slug, int SortOrder);
