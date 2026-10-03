using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Team.Application.TeamItems;

public sealed record TeamEducationPathItemDto(string Year, string Title, string Place);

public static class TeamEducationPathJson
{
    public const int MaxItems = 20;
    public const int MaxYearLength = 32;
    public const int MaxTitleLength = 300;
    public const int MaxPlaceLength = 300;
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

    public static IReadOnlyList<TeamEducationPathItemDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new TeamEducationPathItemDto(
                x.Year ?? string.Empty,
                x.Title ?? string.Empty,
                x.Place ?? string.Empty)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<TeamEducationPathItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new ItemPayload { Year = x.Year, Title = x.Title, Place = x.Place }),
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
            error = "مسیر تخصصی خیلی طولانی است.";
            return false;
        }

        List<ItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت مسیر تخصصی نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} مورد مسیر تخصصی مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var year = (item.Year ?? string.Empty).Trim();
            var title = (item.Title ?? string.Empty).Trim();
            var place = (item.Place ?? string.Empty).Trim();
            if (year.Length == 0 && title.Length == 0 && place.Length == 0)
                continue;
            if (year.Length == 0 || title.Length == 0 || place.Length == 0)
            {
                error = "برای هر مورد مسیر تخصصی، سال، عنوان و محل الزامی است.";
                return false;
            }

            if (year.Length > MaxYearLength)
            {
                error = $"سال مسیر تخصصی حداکثر {MaxYearLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (title.Length > MaxTitleLength)
            {
                error = $"عنوان مسیر تخصصی حداکثر {MaxTitleLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (place.Length > MaxPlaceLength)
            {
                error = $"محل مسیر تخصصی حداکثر {MaxPlaceLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<TeamEducationPathItemDto> Normalize(IEnumerable<TeamEducationPathItemDto> items)
    {
        var result = new List<TeamEducationPathItemDto>();
        foreach (var item in items)
        {
            var year = (item.Year ?? string.Empty).Trim();
            var title = (item.Title ?? string.Empty).Trim();
            var place = (item.Place ?? string.Empty).Trim();
            if (year.Length == 0 || title.Length == 0 || place.Length == 0)
                continue;

            if (year.Length > MaxYearLength)
                year = year[..MaxYearLength];
            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];
            if (place.Length > MaxPlaceLength)
                place = place[..MaxPlaceLength];

            result.Add(new TeamEducationPathItemDto(year, title, place));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private sealed class ItemPayload
    {
        public string? Year { get; set; }
        public string? Title { get; set; }
        public string? Place { get; set; }
    }
}
