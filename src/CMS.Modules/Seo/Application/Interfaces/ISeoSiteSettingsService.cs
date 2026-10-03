using CMS.Modules.Seo.Application.Settings;

namespace CMS.Modules.Seo.Application.Interfaces;

public interface ISeoSiteSettingsService
{
    Task<SeoSiteSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateSeoSiteSettingsCommand command, CancellationToken cancellationToken = default);
}
