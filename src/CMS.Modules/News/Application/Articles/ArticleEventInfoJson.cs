using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.News.Application.Articles;

public sealed record ArticleEventInfoItemDto(string Label, string Value);

public static class ArticleEventInfoJson
{
    public const int MaxItems = 20;
    public const int MaxLabelLength = 80;
    public const int MaxValueLength = 300;
    public const int MaxJsonLength = 20_000;

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

    public static IReadOnlyList<ArticleEventInfoItemDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new ArticleEventInfoItemDto(
                x.Label ?? string.Empty,
                x.Value ?? string.Empty)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<ArticleEventInfoItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new ItemPayload { Label = x.Label, Value = x.Value }),
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
            error = "اطلاعات رویداد خیلی طولانی است.";
            return false;
        }

        List<ItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت اطلاعات رویداد نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} مورد اطلاعات رویداد مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var label = (item.Label ?? string.Empty).Trim();
            var value = (item.Value ?? string.Empty).Trim();

            if (label.Length == 0 && value.Length == 0)
                continue;

            if (label.Length == 0)
            {
                error = "برچسب اطلاعات رویداد الزامی است.";
                return false;
            }

            if (value.Length == 0)
            {
                error = "مقدار اطلاعات رویداد الزامی است.";
                return false;
            }

            if (label.Length > MaxLabelLength)
            {
                error = $"برچسب اطلاعات رویداد حداکثر {MaxLabelLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (value.Length > MaxValueLength)
            {
                error = $"مقدار اطلاعات رویداد حداکثر {MaxValueLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<ArticleEventInfoItemDto> Normalize(IEnumerable<ArticleEventInfoItemDto> items)
    {
        var result = new List<ArticleEventInfoItemDto>();

        foreach (var item in items)
        {
            var label = (item.Label ?? string.Empty).Trim();
            var value = (item.Value ?? string.Empty).Trim();
            if (label.Length == 0 || value.Length == 0)
                continue;

            if (label.Length > MaxLabelLength)
                label = label[..MaxLabelLength];
            if (value.Length > MaxValueLength)
                value = value[..MaxValueLength];

            result.Add(new ArticleEventInfoItemDto(label, value));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private sealed class ItemPayload
    {
        public string? Label { get; set; }
        public string? Value { get; set; }
    }
}
