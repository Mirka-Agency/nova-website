using CMS.Modules.Blog.Application.Categories;

namespace CMS.Modules.Blog.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<CategoryDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveCategoryCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveCategoryCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
