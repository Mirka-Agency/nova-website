using CMS.Application.Payments;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Payments;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class PaymentConfigService : IPaymentConfigService
{
    private readonly ShopDbContext _db;
    private readonly IPaymentSettingsProtector _protector;
    private readonly IValidator<SavePaymentProviderCommand> _validator;
    private readonly IHostEnvironment _environment;

    private static readonly (PaymentProviderType Type, string Name, int Sort)[] Defaults =
    [
        (PaymentProviderType.Zarinpal, "زرین‌پال", 0),
        (PaymentProviderType.Zibal, "زیبال", 1),
        (PaymentProviderType.Sep, "سپ (سامان)", 2),
        (PaymentProviderType.Mellat, "به‌پرداخت ملت", 3),
        (PaymentProviderType.SnapPay, "اسنپ‌پی", 4)
    ];

    public PaymentConfigService(
        ShopDbContext db,
        IPaymentSettingsProtector protector,
        IValidator<SavePaymentProviderCommand> validator,
        IHostEnvironment environment)
    {
        _db = db;
        _protector = protector;
        _validator = validator;
        _environment = environment;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.PaymentProviderConfigs
            .Select(x => x.ProviderType)
            .ToListAsync(cancellationToken);

        var defaultSandbox = !_environment.IsProduction();

        foreach (var (type, name, sort) in Defaults)
        {
            if (existing.Contains(type))
                continue;

            var encrypted = _protector.Protect(PaymentSettingsSerializer.Serialize(new PaymentProviderSettingsDto()));
            var config = PaymentProviderConfig.Create(type, name, isSandbox: defaultSandbox, sort);
            config.Update(name, isEnabled: false, isSandbox: defaultSandbox, sort, encrypted);
            _db.PaymentProviderConfigs.Add(config);
        }

        if (_db.ChangeTracker.HasChanges())
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentProviderConfigDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var items = await _db.PaymentProviderConfigs
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PaymentProviderConfigDto>> ListEnabledAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var items = await _db.PaymentProviderConfigs
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<PaymentProviderConfigDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.PaymentProviderConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<PaymentProviderConfigDto?> GetByTypeAsync(PaymentProviderType type, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var item = await _db.PaymentProviderConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProviderType == type, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task UpdateAsync(Guid id, SavePaymentProviderCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var config = await _db.PaymentProviderConfigs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PaymentProviderConfig), id);

        if (command.IsSandbox && _environment.IsProduction())
            throw new DomainException("در محیط Production امکان استفاده از حالت سندباکس وجود ندارد.");

        var effectiveSandbox = PaymentSettingsSerializer.EffectiveSandbox(command.IsSandbox, _environment.IsProduction());

        var existing = PaymentSettingsSerializer.Deserialize(
            SafeUnprotect(config.EncryptedSettings));
        var merged = MergeSettings(existing, command.Settings);

        if (command.IsEnabled)
            ValidateRequiredSettings(config.ProviderType, effectiveSandbox, merged);

        var encrypted = _protector.Protect(PaymentSettingsSerializer.Serialize(merged));
        config.Update(command.DisplayName, command.IsEnabled, effectiveSandbox, command.SortOrder, encrypted);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private string SafeUnprotect(string encrypted)
    {
        try
        {
            return _protector.Unprotect(encrypted);
        }
        catch
        {
            return "{}";
        }
    }

    private static PaymentProviderSettingsDto MergeSettings(
        PaymentProviderSettingsDto existing,
        PaymentProviderSettingsDto incoming) =>
        new()
        {
            MerchantId = incoming.MerchantId,
            TerminalId = incoming.TerminalId,
            Username = incoming.Username,
            Password = string.IsNullOrWhiteSpace(incoming.Password) ? existing.Password : incoming.Password,
            ClientId = incoming.ClientId,
            ClientSecret = string.IsNullOrWhiteSpace(incoming.ClientSecret) ? existing.ClientSecret : incoming.ClientSecret
        };

    private static void ValidateRequiredSettings(
        PaymentProviderType type,
        bool isSandbox,
        PaymentProviderSettingsDto settings)
    {
        // Zarinpal/Zibal have documented sandbox merchant IDs when credentials are empty.
        // SEP, Mellat, and SnapPay do not publish public sandbox credentials.
        var missing = type switch
        {
            PaymentProviderType.Zarinpal when !isSandbox && string.IsNullOrWhiteSpace(settings.MerchantId)
                => "MerchantId",
            PaymentProviderType.Zibal when !isSandbox && string.IsNullOrWhiteSpace(settings.MerchantId)
                => "MerchantId",
            PaymentProviderType.Sep when string.IsNullOrWhiteSpace(settings.TerminalId)
                => "TerminalId",
            PaymentProviderType.Mellat when string.IsNullOrWhiteSpace(settings.TerminalId)
                || string.IsNullOrWhiteSpace(settings.Username)
                || string.IsNullOrWhiteSpace(settings.Password)
                => "TerminalId/Username/Password",
            PaymentProviderType.SnapPay when string.IsNullOrWhiteSpace(settings.ClientId)
                || string.IsNullOrWhiteSpace(settings.ClientSecret)
                || string.IsNullOrWhiteSpace(settings.Username)
                || string.IsNullOrWhiteSpace(settings.Password)
                => "ClientId/ClientSecret/Username/Password",
            _ => null
        };

        if (missing is not null)
            throw new DomainException($"تنظیمات درگاه ناقص است: {missing}");
    }

    private PaymentProviderConfigDto Map(PaymentProviderConfig config)
    {
        string json;
        try
        {
            json = _protector.Unprotect(config.EncryptedSettings);
        }
        catch
        {
            json = "{}";
        }

        return new PaymentProviderConfigDto(
            config.Id,
            config.ProviderType,
            config.DisplayName,
            config.IsEnabled,
            config.IsSandbox,
            config.SortOrder,
            PaymentSettingsSerializer.Deserialize(json));
    }
}
