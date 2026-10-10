using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CMS.Modules.Blog.Application.Posts;

public sealed record PostVideoSchemaItemDto(
    string Title,
    string ContentUrl,
    string? ThumbnailUrl,
    string? UploadDate,
    int DurationMinutes,
    int DurationSeconds,
    string? Description);

public static class PostVideoSchemaJson
{
    public const int MaxItems = 20;
    public const int MaxTitleLength = 300;
    public const int MaxUrlLength = 1000;
    public const int MaxDescriptionLength = 5000;
    public const int MaxJsonLength = 100_000;
    public const int MaxDurationMinutes = 24 * 60;
    public const int MaxDurationSeconds = 59;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly Regex EmbedUrlHint = new(
        @"youtube\.com|youtu\.be|aparat\.com|vimeo\.com|/embed/|player\.",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<PostVideoSchemaItemDto> Parse(string? videoSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(videoSchemaJson))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<VideoSchemaItemPayload>>(videoSchemaJson, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(ToDto));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<PostVideoSchemaItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new VideoSchemaItemPayload
            {
                Title = x.Title,
                ContentUrl = x.ContentUrl,
                ThumbnailUrl = x.ThumbnailUrl,
                UploadDate = x.UploadDate,
                DurationMinutes = x.DurationMinutes,
                DurationSeconds = x.DurationSeconds,
                Description = x.Description
            }),
            SerializerOptions);
    }

    public static string? NormalizeJson(string? videoSchemaJson)
    {
        if (string.IsNullOrWhiteSpace(videoSchemaJson))
            return null;

        return Serialize(Parse(videoSchemaJson));
    }

    public static bool TryValidate(string? videoSchemaJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(videoSchemaJson))
            return true;

        if (videoSchemaJson.Length > MaxJsonLength)
        {
            error = "اسکیمای ویدیو خیلی طولانی است.";
            return false;
        }

        List<VideoSchemaItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<VideoSchemaItemPayload>>(videoSchemaJson, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت اسکیمای ویدیو نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} ویدیو برای اسکیما مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var contentUrl = (item.ContentUrl ?? string.Empty).Trim();
            var thumbnailUrl = (item.ThumbnailUrl ?? string.Empty).Trim();
            var description = (item.Description ?? string.Empty).Trim();
            var uploadDate = (item.UploadDate ?? string.Empty).Trim();
            var minutes = item.DurationMinutes ?? 0;
            var seconds = item.DurationSeconds ?? 0;

            if (title.Length == 0 && contentUrl.Length == 0 && thumbnailUrl.Length == 0
                && description.Length == 0 && uploadDate.Length == 0 && minutes == 0 && seconds == 0)
            {
                continue;
            }

            if (title.Length == 0)
            {
                error = "عنوان ویدیو الزامی است.";
                return false;
            }

            if (contentUrl.Length == 0)
            {
                error = "لینک ویدیو الزامی است.";
                return false;
            }

            if (title.Length > MaxTitleLength)
            {
                error = $"عنوان ویدیو حداکثر {MaxTitleLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (contentUrl.Length > MaxUrlLength || !IsValidUrlOrPath(contentUrl))
            {
                error = "لینک ویدیو نامعتبر است.";
                return false;
            }

            if (thumbnailUrl.Length > 0 && (thumbnailUrl.Length > MaxUrlLength || !IsValidUrlOrPath(thumbnailUrl)))
            {
                error = "لینک تصویر کاور نامعتبر است.";
                return false;
            }

            if (description.Length > MaxDescriptionLength)
            {
                error = $"توضیحات ویدیو حداکثر {MaxDescriptionLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (uploadDate.Length > 0 && !TryNormalizeUploadDate(uploadDate, out _))
            {
                error = "تاریخ انتشار ویدیو نامعتبر است.";
                return false;
            }

            if (minutes < 0 || minutes > MaxDurationMinutes)
            {
                error = "دقیقه مدت زمان نامعتبر است.";
                return false;
            }

            if (seconds < 0 || seconds > MaxDurationSeconds)
            {
                error = "ثانیه مدت زمان باید بین ۰ تا ۵۹ باشد.";
                return false;
            }
        }

        return true;
    }

    public static string? BuildJsonLd(IEnumerable<PostVideoSchemaItemDto>? items, string? pageUrl = null)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        var videoObjects = normalized.Select(item => BuildVideoObject(item, pageUrl)).ToList();
        object payload = videoObjects.Count == 1
            ? videoObjects[0]
            : new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = videoObjects.Select(StripContext).ToList()
            };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private static Dictionary<string, object?> BuildVideoObject(PostVideoSchemaItemDto item, string? pageUrl)
    {
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "VideoObject",
            ["name"] = item.Title
        };

        if (!string.IsNullOrWhiteSpace(item.Description))
            payload["description"] = item.Description;

        if (!string.IsNullOrWhiteSpace(item.ThumbnailUrl))
            payload["thumbnailUrl"] = item.ThumbnailUrl;

        if (!string.IsNullOrWhiteSpace(item.UploadDate))
            payload["uploadDate"] = item.UploadDate;

        var duration = ToIso8601Duration(item.DurationMinutes, item.DurationSeconds);
        if (duration is not null)
            payload["duration"] = duration;

        if (!string.IsNullOrWhiteSpace(pageUrl))
            payload["url"] = pageUrl.Trim();

        if (LooksLikeEmbedUrl(item.ContentUrl))
            payload["embedUrl"] = item.ContentUrl;
        else
            payload["contentUrl"] = item.ContentUrl;

        return payload;
    }

    private static Dictionary<string, object?> StripContext(Dictionary<string, object?> source)
    {
        var copy = new Dictionary<string, object?>(source);
        copy.Remove("@context");
        return copy;
    }

    private static IReadOnlyList<PostVideoSchemaItemDto> Normalize(IEnumerable<PostVideoSchemaItemDto> items)
    {
        var result = new List<PostVideoSchemaItemDto>();
        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var contentUrl = (item.ContentUrl ?? string.Empty).Trim();
            if (title.Length == 0 || contentUrl.Length == 0)
                continue;

            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];
            if (contentUrl.Length > MaxUrlLength)
                contentUrl = contentUrl[..MaxUrlLength];

            var thumbnailUrl = NullIfWhiteSpace(item.ThumbnailUrl);
            if (thumbnailUrl is { Length: > MaxUrlLength })
                thumbnailUrl = thumbnailUrl[..MaxUrlLength];

            var description = NullIfWhiteSpace(item.Description);
            if (description is { Length: > MaxDescriptionLength })
                description = description[..MaxDescriptionLength];

            string? uploadDate = null;
            if (!string.IsNullOrWhiteSpace(item.UploadDate)
                && TryNormalizeUploadDate(item.UploadDate, out var normalizedDate))
            {
                uploadDate = normalizedDate;
            }

            var minutes = Math.Clamp(item.DurationMinutes, 0, MaxDurationMinutes);
            var seconds = Math.Clamp(item.DurationSeconds, 0, MaxDurationSeconds);

            result.Add(new PostVideoSchemaItemDto(
                title,
                contentUrl,
                thumbnailUrl,
                uploadDate,
                minutes,
                seconds,
                description));

            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private static PostVideoSchemaItemDto ToDto(VideoSchemaItemPayload item) =>
        new(
            item.Title ?? string.Empty,
            item.ContentUrl ?? string.Empty,
            item.ThumbnailUrl,
            item.UploadDate,
            item.DurationMinutes ?? 0,
            item.DurationSeconds ?? 0,
            item.Description);

    public static string? ToIso8601Duration(int minutes, int seconds)
    {
        minutes = Math.Clamp(minutes, 0, MaxDurationMinutes);
        seconds = Math.Clamp(seconds, 0, MaxDurationSeconds);
        if (minutes == 0 && seconds == 0)
            return null;

        if (minutes == 0)
            return $"PT{seconds}S";
        if (seconds == 0)
            return $"PT{minutes}M";
        return $"PT{minutes}M{seconds}S";
    }

    public static bool TryNormalizeUploadDate(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (DateTime.TryParseExact(
                trimmed,
                ["yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            normalized = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
        {
            normalized = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static bool LooksLikeEmbedUrl(string url) => EmbedUrlHint.IsMatch(url);

    private static bool IsValidUrlOrPath(string value)
    {
        if (value.StartsWith('/'))
            return value.Length > 1;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class VideoSchemaItemPayload
    {
        public string? Title { get; set; }
        public string? ContentUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? UploadDate { get; set; }
        public int? DurationMinutes { get; set; }
        public int? DurationSeconds { get; set; }
        public string? Description { get; set; }
    }
}
