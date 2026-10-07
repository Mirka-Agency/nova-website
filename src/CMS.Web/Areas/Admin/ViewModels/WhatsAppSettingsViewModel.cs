using System.ComponentModel.DataAnnotations;
using CMS.Application.Messaging;
using CMS.Application.WhatsApp;

namespace CMS.Web.Areas.Admin.ViewModels;

public sealed class WhatsAppSettingsPageViewModel
{
    public required WhatsAppSettingsFormViewModel Settings { get; init; }
    public required WhatsAppStatusDto Status { get; init; }
    public required IReadOnlyList<WhatsAppGroupDto> Groups { get; init; }
    public required IReadOnlyList<MessageLogDto> RecentLogs { get; init; }
    public string? ServiceWarning { get; init; }
}

public sealed class WhatsAppSettingsFormViewModel
{
    public bool Enabled { get; set; }

    [StringLength(200)]
    public string? DefaultGroupId { get; set; }

    [StringLength(300)]
    public string? DefaultGroupName { get; set; }

    [StringLength(8000)]
    public string? DefaultTemplate { get; set; }

    public DateTime? LastSuccessfulSendAtUtc { get; set; }
}

public sealed class WhatsAppTestMessageRequest
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; set; } = "پیام آزمایشی از پنل مدیریت میرکا";

    [StringLength(200)]
    public string? GroupId { get; set; }
}
