namespace CMS.Web.Areas.Admin.ViewModels;

public sealed record DashboardViewModel(IReadOnlyList<DashboardStatCardViewModel> Cards);

public sealed record DashboardStatCardViewModel(
    string Title,
    int Count,
    string? Url,
    string ActionLabel,
    string IconCss = "fas fa-chart-bar");
