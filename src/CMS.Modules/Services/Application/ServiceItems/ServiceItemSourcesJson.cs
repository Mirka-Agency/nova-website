using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Services.Application.ServiceItems;

public sealed record ServiceItemSourceItemDto(string Title, string Url);

public static class ServiceItemSourcesJson
{
    public const int MaxItems = 50;
    public const int MaxTitleLength = 500;
    public const int MaxUrlLength = 1000;
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

    public static IReadOnlyList<ServiceItemSourceItemDto> Parse(string? sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<SourceItemPayload>>(sourcesJson, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new ServiceItemSourceItemDto(x.Title ?? string.Empty, x.Url ?? string.Empty)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<ServiceItemSourceItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new SourceItemPayload { Title = x.Title, Url = x.Url }),
            SerializerOptions);
    }

    public static string? NormalizeJson(string? sourcesJson)
    {
        if (string.IsNullOrWhiteSpace(sourcesJson))
            return null;

        return Serialize(Parse(sourcesJson));
    }

    public static bool TryValidate(string? sourcesJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sourcesJson))
            return true;

        if (sourcesJson.Length > MaxJsonLength)
        {
            error = "منابع علمی خیلی طولانی است.";
            return false;
        }

        List<SourceItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<SourceItemPayload>>(sourcesJson, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت منابع علمی نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} منبع علمی مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var url = (item.Url ?? string.Empty).Trim();
            if (title.Length == 0 && url.Length == 0)
                continue;
            if (title.Length == 0 || url.Length == 0)
            {
                error = "برای هر منبع علمی، هم عنوان و هم لینک الزامی است.";
                return false;
            }

            if (title.Length > MaxTitleLength)
            {
                error = $"عنوان منبع حداکثر {MaxTitleLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (url.Length > MaxUrlLength)
            {
                error = $"لینک منبع حداکثر {MaxUrlLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (!IsValidUrl(url))
            {
                error = "لینک منبع باید آدرس مطلق http یا https باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<ServiceItemSourceItemDto> Normalize(IEnumerable<ServiceItemSourceItemDto> items)
    {
        var result = new List<ServiceItemSourceItemDto>();
        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var url = (item.Url ?? string.Empty).Trim();
            if (title.Length == 0 || url.Length == 0 || !IsValidUrl(url))
                continue;

            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];
            if (url.Length > MaxUrlLength)
                url = url[..MaxUrlLength];

            result.Add(new ServiceItemSourceItemDto(title, url));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private static bool IsValidUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private sealed class SourceItemPayload
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
    }
}
