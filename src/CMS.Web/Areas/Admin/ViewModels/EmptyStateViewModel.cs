namespace CMS.Web.Areas.Admin.ViewModels;

public sealed record EmptyStateViewModel(
    string Message,
    string? ActionUrl = null,
    string? ActionLabel = null);
