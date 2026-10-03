using CMS.Application.Messaging;

namespace CMS.Web.Areas.Admin.ViewModels;

public sealed class MessageLogsDashboardViewModel
{
    public string Period { get; init; } = "7d";
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public required MessageLogPeriodStatsDto Stats { get; init; }
    public required IReadOnlyList<MessageLogDto> Recent { get; init; }
}
