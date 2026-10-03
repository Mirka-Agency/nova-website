using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.News.Application.Articles;

public sealed record ArticleGalleryImageDto(string Url, string? AltText);

public static class ArticleGalleryJson
{
    public const int MaxItems = 30;
    public const int MaxUrlLength = 1000;
    public const int MaxAltTextLength = 300;
    public const int MaxJsonLength = 100_000;

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

    public static IReadOnlyList<ArticleGalleryImageDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new ArticleGalleryImageDto(
                x.Url ?? string.Empty,
                x.AltText)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<ArticleGalleryImageDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new ItemPayload { Url = x.Url, AltText = x.AltText }),
            SerializerOptions);
    }

    public static string? NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return Serialize(Parse(json));
    }

    public static bool TryValidate(string? json, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(json))
            return true;

        if (json.Length > MaxJsonLength)
        {
            error = "گالری تصاویر خیلی طولانی است.";
            return false;
        }

        List<ItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت گالری تصاویر نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} تصویر در گالری مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var url = (item.Url ?? string.Empty).Trim();
            if (url.Length == 0)
                continue;

            if (url.Length > MaxUrlLength)
            {
                error = $"آدرس تصویر گالری حداکثر {MaxUrlLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (!IsValidUrlOrPath(url))
            {
                error = "آدرس تصویر گالری باید مطلق یا مسیر نسبی سایت باشد.";
                return false;
            }

            var alt = (item.AltText ?? string.Empty).Trim();
            if (alt.Length > MaxAltTextLength)
            {
                error = $"متن جایگزین تصویر گالری حداکثر {MaxAltTextLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<ArticleGalleryImageDto> Normalize(IEnumerable<ArticleGalleryImageDto> items)
    {
        var result = new List<ArticleGalleryImageDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var url = (item.Url ?? string.Empty).Trim();
            if (url.Length == 0 || !seen.Add(url))
                continue;

            if (url.Length > MaxUrlLength)
                url = url[..MaxUrlLength];

            string? alt = string.IsNullOrWhiteSpace(item.AltText)
                ? null
                : item.AltText.Trim();
            if (alt is { Length: > MaxAltTextLength })
                alt = alt[..MaxAltTextLength];

            result.Add(new ArticleGalleryImageDto(url, alt));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private static bool IsValidUrlOrPath(string value)
    {
        if (value.StartsWith('/'))
            return value.Length > 1;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private sealed class ItemPayload
    {
        public string? Url { get; set; }
        public string? AltText { get; set; }
    }
}
