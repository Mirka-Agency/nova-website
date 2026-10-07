using CMS.Application.WhatsApp;
using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Infrastructure.WhatsApp;

public sealed class WhatsAppSettings : BaseEntity
{
    public static readonly Guid SingletonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001");

    private WhatsAppSettings()
    {
    }

    public bool Enabled { get; private set; }
    public string? DefaultGroupId { get; private set; }
    public string? DefaultGroupName { get; private set; }
    public string DefaultTemplate { get; private set; } = WhatsAppDefaultTemplate.Value;
    public DateTime? LastSuccessfulSendAtUtc { get; private set; }

    public static WhatsAppSettings CreateDefault() => new()
    {
        Id = SingletonId,
        Enabled = false,
        DefaultTemplate = WhatsAppDefaultTemplate.Value
    };

    public void Update(bool enabled, string? defaultGroupId, string? defaultGroupName, string? defaultTemplate)
    {
        Enabled = enabled;
        DefaultGroupId = NormalizeOptional(defaultGroupId, 200);
        DefaultGroupName = NormalizeOptional(defaultGroupName, 300);

        if (string.IsNullOrWhiteSpace(defaultTemplate))
        {
            DefaultTemplate = WhatsAppDefaultTemplate.Value;
        }
        else
        {
            var trimmed = defaultTemplate.Trim();
            if (trimmed.Length > 8000)
                throw new DomainException("قالب پیام واتساپ خیلی طولانی است.");
            DefaultTemplate = trimmed;
        }

        Touch();
    }

    public void MarkSuccessfulSend(DateTime utcNow)
    {
        LastSuccessfulSendAtUtc = utcNow;
        Touch();
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
