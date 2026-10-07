using CMS.Application.WhatsApp;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CMS.Infrastructure.WhatsApp;

public sealed class WhatsAppSettingsService : IWhatsAppSettingsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<WhatsAppSettingsService> _logger;

    public WhatsAppSettingsService(ApplicationDbContext db, ILogger<WhatsAppSettingsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WhatsAppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateAsync(cancellationToken);
        return Map(entity);
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
    }

    public async Task MarkSuccessfulSendAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetOrCreateAsync(cancellationToken);
            entity.MarkSuccessfulSend(DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
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
