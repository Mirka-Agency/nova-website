using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Settings;
using CMS.Modules.Seo.Domain.Entities;
using CMS.Modules.Seo.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Seo.Infrastructure.Services;

public sealed class SeoSiteSettingsService : ISeoSiteSettingsService
{
    private readonly SeoDbContext _db;
    private readonly IValidator<UpdateSeoSiteSettingsCommand> _validator;

    public SeoSiteSettingsService(SeoDbContext db, IValidator<UpdateSeoSiteSettingsCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<SeoSiteSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await EnsureAsync(cancellationToken);
        return Map(settings);
    }

    public async Task UpdateAsync(UpdateSeoSiteSettingsCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var settings = await EnsureAsync(cancellationToken);
        settings.Update(
            command.OrganizationName,
            command.OrganizationUrl,
            command.OrganizationLogoUrl,
            command.DefaultSchemaType,
            command.RobotsTxt,
            command.TwitterSiteHandle,
            command.EnableBrokenLinkChecks,
            command.SitemapEnabled);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<SeoSiteSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SeoSiteSettings.SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = SeoSiteSettings.CreateDefault();
            _db.SiteSettings.Add(settings);
            await _db.SaveChangesAsync(cancellationToken);
            return settings;
        }

        if (settings.EnsureFullRobotsTxt())
            await _db.SaveChangesAsync(cancellationToken);

        return settings;
    }

    private async Task ValidateAsync(UpdateSeoSiteSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (result.IsValid)
            return;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        throw new DomainValidationException(errors);
    }

    private static SeoSiteSettingsDto Map(SeoSiteSettings s) =>
        new(
            s.Id,
            s.OrganizationName,
            s.OrganizationUrl,
            s.OrganizationLogoUrl,
            s.DefaultSchemaType,
            s.RobotsTxt ?? SeoSiteSettings.DefaultRobotsTxt,
            s.TwitterSiteHandle,
            s.EnableBrokenLinkChecks,
            s.SitemapEnabled,
            s.CreatedAtUtc,
            s.UpdatedAtUtc);
}
