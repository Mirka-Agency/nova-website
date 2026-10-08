using System.Text.RegularExpressions;

namespace CMS.Modules.Video.Application.VideoItems;

/// <summary>
/// Resolves a public thumbnail image URL from a hosted video link (YouTube / Vimeo).
/// Aparat posters require an API lookup — see <c>IVideoThumbnailResolver</c>.
/// </summary>
public static partial class VideoThumbnailUrl
{
    public static string? PreferCover(string? coverImageUrl, string? videoUrl)
    {
        if (!string.IsNullOrWhiteSpace(coverImageUrl))
            return coverImageUrl.Trim();

        return Resolve(videoUrl);
    }

    public static string? Resolve(string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return null;

        var raw = NormalizeRaw(videoUrl);

        var yt = YouTubeIdRegex().Match(raw);
        if (yt.Success)
            return $"https://img.youtube.com/vi/{yt.Groups[1].Value}/hqdefault.jpg";

        var vimeo = VimeoIdRegex().Match(raw);
        if (vimeo.Success)
            return $"https://vumbnail.com/{vimeo.Groups[1].Value}.jpg";

        return null;
    }

    public static string? TryGetAparatHash(string? videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            return null;

        var raw = NormalizeRaw(videoUrl);
        var aparat = AparatHashRegex().Match(raw);
        return aparat.Success ? aparat.Groups[1].Value : null;
    }

    private static string NormalizeRaw(string url)
    {
        var raw = url.Trim();
        var iframeSrc = IframeSrcRegex().Match(raw);
        if (iframeSrc.Success)
            raw = iframeSrc.Groups[1].Value.Trim();
        return raw;
    }

    [GeneratedRegex(
        @"src\s*=\s*[""'](?<src>https?://[^""']+)[""']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IframeSrcRegex();

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
