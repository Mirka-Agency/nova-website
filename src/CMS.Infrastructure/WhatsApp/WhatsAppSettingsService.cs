using CMS.Application.WhatsApp;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.WhatsApp;

public sealed class WhatsAppSettingsService : IWhatsAppSettingsService
{
    private const string CacheKey = "cms:whatsapp-settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(120);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WhatsAppSettingsService> _logger;

    public WhatsAppSettingsService(
        ApplicationDbContext db,
        IMemoryCache cache,
        ILogger<WhatsAppSettingsService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<WhatsAppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out WhatsAppSettingsDto? cached) && cached is not null)
            return cached;

        var entity = await GetOrCreateAsync(cancellationToken);
        var dto = Map(entity);
        _cache.Set(CacheKey, dto, CacheDuration);
        return dto;
    }

    public async Task UpdateAsync(UpdateWhatsAppSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateAsync(cancellationToken);
        entity.Update(
            command.Enabled,
            command.DefaultGroupId,
            command.DefaultGroupName,
            command.DefaultTemplate);
        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    public async Task MarkSuccessfulSendAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetOrCreateAsync(cancellationToken);
            entity.MarkSuccessfulSend(DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            _cache.Remove(CacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update WhatsApp last successful send timestamp");
        }
    }

    private async Task<WhatsAppSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var entity = await _db.WhatsAppSettings
            .FirstOrDefaultAsync(x => x.Id == WhatsAppSettings.SingletonId, cancellationToken);

        if (entity is not null)
            return entity;

        entity = WhatsAppSettings.CreateDefault();
        _db.WhatsAppSettings.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static WhatsAppSettingsDto Map(WhatsAppSettings entity) =>
        new(
            entity.Enabled,
            entity.DefaultGroupId,
            entity.DefaultGroupName,
            entity.DefaultTemplate,
            entity.LastSuccessfulSendAtUtc,
            entity.UpdatedAtUtc);
}
