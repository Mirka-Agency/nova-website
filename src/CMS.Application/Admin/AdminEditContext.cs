namespace CMS.Application.Admin;

/// <summary>
/// Ambient edit target for the public-site admin bar (WordPress-style).
/// Set from public controllers via <see cref="ViewDataKey"/>.
/// </summary>
public sealed class AdminEditContext
{
    public const string ViewDataKey = "AdminEditContext";

    public required string Label { get; init; }
    public required string Controller { get; init; }
    public string Action { get; init; } = "Edit";
    public Guid? Id { get; init; }
    public string Area { get; init; } = "Admin";

    /// <summary>Authorization policy required to show this link (e.g. ManageBlog).</summary>
    public string? RequiredPolicy { get; init; }

    public static AdminEditContext Edit(string controller, Guid id, string label, string requiredPolicy) =>
        new()
        {
            Controller = controller,
            Id = id,
            Label = label,
            Action = "Edit",
            RequiredPolicy = requiredPolicy
        };

    public static AdminEditContext Manage(string controller, string label, string requiredPolicy) =>
        new()
        {
            Controller = controller,
            Label = label,
            Action = "Index",
            RequiredPolicy = requiredPolicy
        };

    public static AdminEditContext SiteSettings() =>
        new()
        {
            Controller = "Settings",
            Action = "Index",
            Label = "ویرایش تنظیمات سایت",
            RequiredPolicy = "AdminOnly"
        };
}
