using CMS.Application.Common.Paging;
using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Public;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Interfaces;

public interface IFormService
{
    Task<IReadOnlyList<FormListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<FormListItemDto>> ListPagedAsync(FormListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<FormDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveFormCommand command, CancellationToken cancellationToken = default);
    Task<Guid> CreateFromTemplateAsync(FormTemplateKind template, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveFormCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> DuplicateAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetStatusAsync(Guid id, FormStatus status, CancellationToken cancellationToken = default);
    Task RestoreVersionAsync(Guid formId, Guid versionId, CancellationToken cancellationToken = default);
    Task<Guid> AddFieldAsync(Guid formId, SaveFormFieldCommand command, CancellationToken cancellationToken = default);
    Task UpdateFieldAsync(Guid formId, Guid fieldId, SaveFormFieldCommand command, CancellationToken cancellationToken = default);
    Task<Guid> DuplicateFieldAsync(Guid formId, Guid fieldId, CancellationToken cancellationToken = default);
    Task DeleteFieldAsync(Guid formId, Guid fieldId, CancellationToken cancellationToken = default);
    Task ReorderFieldsAsync(Guid formId, ReorderFieldsCommand command, CancellationToken cancellationToken = default);
    Task<FormPublicContract?> GetPublicContractBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<FormPublicContract?> GetPublicContractByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<FormPublicContract?> GetPublicContractByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
