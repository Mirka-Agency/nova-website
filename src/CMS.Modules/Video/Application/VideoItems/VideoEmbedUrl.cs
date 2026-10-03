using System.Text.RegularExpressions;

namespace CMS.Modules.Video.Application.VideoItems;

/// <summary>Normalizes Aparat/YouTube/Vimeo share or embed URLs into a playable iframe src.</summary>
public static partial class VideoEmbedUrl
{
    public static string? Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var raw = url.Trim();

        // Paste of full iframe HTML from Aparat share box.
        var iframeSrc = IframeSrcRegex().Match(raw);
        if (iframeSrc.Success)
            raw = iframeSrc.Groups[1].Value.Trim();

        if (raw.Contains("aparat.com/video/video/embed/", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("youtube.com/embed/", StringComparison.OrdinalIgnoreCase)
            || raw.Contains("player.vimeo.com/video/", StringComparison.OrdinalIgnoreCase))
        {
            return StripFragment(raw);
        }

        var aparat = AparatHashRegex().Match(raw);
        if (aparat.Success)
        {
            var hash = aparat.Groups[1].Value;
            return $"https://www.aparat.com/video/video/embed/videohash/{hash}/vt/frame";
        }

        var yt = YouTubeIdRegex().Match(raw);
        if (yt.Success)
            return $"https://www.youtube.com/embed/{yt.Groups[1].Value}";

        var vimeo = VimeoIdRegex().Match(raw);
        if (vimeo.Success)
            return $"https://player.vimeo.com/video/{vimeo.Groups[1].Value}";

        return null;
    }

    /// <summary>True when the URL looks like a hosted share page (not a direct media file).</summary>
    public static bool IsHostedPage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var raw = url.Trim();
        return raw.Contains("aparat.com", StringComparison.OrdinalIgnoreCase)
               || raw.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
               || raw.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
               || raw.Contains("vimeo.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripFragment(string url)
    {
        var hash = url.IndexOf('#');
        return hash >= 0 ? url[..hash] : url;
    }

    [GeneratedRegex(
        @"src\s*=\s*[""'](?<src>https?://[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IframeSrcRegex();

    // /v/{hash}, /embed/{hash}, embed/videohash/{hash}, embed_box/videohash/{hash}
    [GeneratedRegex(
        @"aparat\.com/(?:v/|embed/|video/video/embed(?:_box)?/videohash/)([A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AparatHashRegex();

    [GeneratedRegex(
        @"(?:youtube\.com/watch\?(?:[^#]*&)?v=|youtube\.com/embed/|youtu\.be/)([A-Za-z0-9_-]{6,})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex YouTubeIdRegex();

    [GeneratedRegex(
        @"vimeo\.com/(?:video/)?(\d+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex VimeoIdRegex();
}
