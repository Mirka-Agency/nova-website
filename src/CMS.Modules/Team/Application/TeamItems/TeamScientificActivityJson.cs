using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Team.Application.TeamItems;

public sealed record TeamScientificActivityItemDto(string Text);

public static class TeamScientificActivityJson
{
    public const int MaxItems = 50;
    public const int MaxTextLength = 1000;
    public const int MaxJsonLength = 50_000;

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

    public static IReadOnlyList<TeamScientificActivityItemDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new TeamScientificActivityItemDto(x.Text ?? string.Empty)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<TeamScientificActivityItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new ItemPayload { Text = x.Text }),
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
            error = "فعالیت علمی خیلی طولانی است.";
            return false;
        }

        List<ItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت فعالیت علمی نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} مورد فعالیت علمی مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var text = (item.Text ?? string.Empty).Trim();
            if (text.Length == 0)
                continue;

            if (text.Length > MaxTextLength)
            {
                error = $"هر مورد فعالیت علمی حداکثر {MaxTextLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<TeamScientificActivityItemDto> Normalize(IEnumerable<TeamScientificActivityItemDto> items)
    {
        var result = new List<TeamScientificActivityItemDto>();
        foreach (var item in items)
        {
            var text = (item.Text ?? string.Empty).Trim();
            if (text.Length == 0)
                continue;

            if (text.Length > MaxTextLength)
                text = text[..MaxTextLength];

            result.Add(new TeamScientificActivityItemDto(text));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private sealed class ItemPayload
    {
        public string? Text { get; set; }
    }
}
