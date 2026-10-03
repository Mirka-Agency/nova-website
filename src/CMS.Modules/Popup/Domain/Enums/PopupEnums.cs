namespace CMS.Modules.Popup.Domain.Enums;

/// <summary>
/// Built-in trigger kinds. Stored as string so new triggers (e.g. exit_intent) can be added without schema churn.
/// </summary>
public static class PopupTriggerTypes
{
    public const string Manual = "manual";
    public const string Timer = "timer";
    public const string Scroll = "scroll";

    public static readonly HashSet<string> Known =
    [
        Manual,
        Timer,
        Scroll
    ];

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Known.Contains(value.Trim().ToLowerInvariant());
}

/// <summary>CTA button actions. Stored as string for future actions without schema churn.</summary>
public static class PopupCtaActions
{
    public const string None = "none";
    public const string Url = "url";
    public const string Close = "close";
    public const string OpenPopup = "open_popup";

    public static readonly HashSet<string> Known =
    [
        None,
        Url,
        Close,
        OpenPopup
    ];

    public static string Normalize(string? value)
    {
        var v = string.IsNullOrWhiteSpace(value) ? None : value.Trim().ToLowerInvariant();
        return Known.Contains(v) ? v : None;
    }
}

public enum PopupFrequency
{
    Always = 0,
    EveryVisit = 1,
    OncePerSession = 2,
    OncePerBrowser = 3
}

public enum PopupPageTargetMode
{
    All = 0,
    Homepage = 1,
    Include = 2,
    Exclude = 3
}
