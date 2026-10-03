using CMS.Application.Common.Paging;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Interfaces;

public interface ISubmissionService
{
    Task<IReadOnlyList<SubmissionListItemDto>> ListAsync(Guid? formId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<SubmissionListItemDto>> ListPagedAsync(SubmissionListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountUnreadAsync(CancellationToken cancellationToken = default);
    Task<SubmissionDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
    Task MarkReadAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkUnreadAsync(Guid id, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task UnarchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task BulkSetStatusAsync(IReadOnlyList<Guid> ids, SubmissionStatus status, CancellationToken cancellationToken = default);
    Task<Stream> ExportCsvAsync(ExportSubmissionsRequest request, CancellationToken cancellationToken = default);
    Task<SubmitResultDto> SubmitAsync(SubmitFormCommand command, CancellationToken cancellationToken = default);
}
