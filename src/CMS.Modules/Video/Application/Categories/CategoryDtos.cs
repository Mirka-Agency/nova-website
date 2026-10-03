namespace CMS.Modules.Video.Application.Categories;

public sealed record CategoryListItemDto(Guid Id, string Name, string Slug, int VideoItemCount, string? ImageUrl);

public sealed record CategoryDetailDto(Guid Id, string Name, string Slug, string Description, string? ImageUrl);

public sealed record SaveCategoryCommand(string Name, string? Slug, string? Description, string? ImageUrl);
