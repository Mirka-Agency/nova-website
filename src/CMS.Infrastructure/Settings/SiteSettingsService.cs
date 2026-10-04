using CMS.Application.Settings;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Infrastructure.Settings;

public sealed class SiteSettingsService : ISiteSettingsService
{
    private const string CacheKey = "cms:site-settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IValidator<UpdateSiteSettingsCommand> _settingsValidator;
    private readonly IValidator<UpdateSiteScriptsCommand> _scriptsValidator;

    public SiteSettingsService(
        ApplicationDbContext db,
        IMemoryCache cache,
        IValidator<UpdateSiteSettingsCommand> settingsValidator,
        IValidator<UpdateSiteScriptsCommand> scriptsValidator)
    {
        _db = db;
        _cache = cache;
        _settingsValidator = settingsValidator;
        _scriptsValidator = scriptsValidator;
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettingsDto? cached) && cached is not null)
            return cached;

        var settings = await EnsureAsync(cancellationToken);
        var dto = Map(settings);
        _cache.Set(CacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task UpdateAsync(UpdateSiteSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _settingsValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.Update(new SiteSettingsValues(
            command.SiteName,
            command.Tagline,
            command.ContactEmail,
            command.ContactPhone,
            command.Address,
            command.BusinessHours,
            command.FooterText,
            command.LogoUrl,
            command.FaviconUrl,
            command.MetaTitle,
            command.MetaDescription,
            command.DefaultOgImageUrl,
            command.InstagramUrl,
            command.TelegramUrl,
            command.TwitterUrl,
            command.LinkedInUrl,
            command.AparatUrl,
            command.FacebookUrl,
            command.YouTubeUrl,
            command.WhatsAppUrl,
            command.PrivacyHtml,
            command.MaintenanceMode,
            command.MaintenanceMessage));
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task UpdateScriptsAsync(UpdateSiteScriptsCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _scriptsValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await EnsureAsync(cancellationToken);
        settings.UpdateScripts(command.HeadScripts, command.BodyOpenScripts, command.BodyCloseScripts);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    private async Task<SiteSettings> EnsureAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.SiteSettings.OrderBy(s => s.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
            return settings;

        settings = SiteSettings.CreateDefault();
        _db.SiteSettings.Add(settings);
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static SiteSettingsDto Map(SiteSettings settings) =>
        new(
            settings.SiteName,
            settings.Tagline,
            settings.ContactEmail,
            settings.ContactPhone,
            settings.Address,
            settings.BusinessHours,
            settings.FooterText,
            settings.LogoUrl,
            settings.FaviconUrl,
            settings.MetaTitle,
            settings.MetaDescription,
            settings.DefaultOgImageUrl,
            settings.InstagramUrl,
            settings.TelegramUrl,
            settings.TwitterUrl,
            settings.LinkedInUrl,
            settings.AparatUrl,
            settings.FacebookUrl,
            settings.YouTubeUrl,
            settings.WhatsAppUrl,
            settings.PrivacyHtml,
            settings.HeadScripts,
            settings.BodyOpenScripts,
            settings.BodyCloseScripts,
            settings.MaintenanceMode,
            settings.MaintenanceMessage);
}
