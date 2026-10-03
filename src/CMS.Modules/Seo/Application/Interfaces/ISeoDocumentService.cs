using CMS.Modules.Seo.Application.Documents;

namespace CMS.Modules.Seo.Application.Interfaces;

public interface ISeoDocumentService
{
    Task<SeoDocumentDto?> GetAsync(string contentType, Guid contentId, CancellationToken cancellationToken = default);
    Task<SeoDocumentDto> UpsertAsync(SaveSeoDocumentCommand command, CancellationToken cancellationToken = default);
}
