using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Team.Application.TeamItems;

public sealed record TeamSpecialtyPathItemDto(string Title, string Text, string? IconUrl = null);

public static class TeamSpecialtyPathJson
{
    public const int MaxItems = 20;
    public const int MaxTitleLength = 200;
    public const int MaxTextLength = 1000;
    public const int MaxIconUrlLength = 1000;
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

    public static IReadOnlyList<TeamSpecialtyPathItemDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new TeamSpecialtyPathItemDto(
                x.Title ?? string.Empty,
                x.Text ?? string.Empty,
                x.IconUrl)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<TeamSpecialtyPathItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new ItemPayload
            {
                Title = x.Title,
                Text = x.Text,
                IconUrl = x.IconUrl
            }),
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
            error = "حوزه فعالیت خیلی طولانی است.";
            return false;
        }

        List<ItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ItemPayload>>(json, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت حوزه فعالیت نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} مورد حوزه فعالیت مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var text = (item.Text ?? string.Empty).Trim();
            var iconUrl = (item.IconUrl ?? string.Empty).Trim();
            if (title.Length == 0 && text.Length == 0 && iconUrl.Length == 0)
                continue;
            if (title.Length == 0)
            {
                error = "برای هر مورد حوزه فعالیت، عنوان الزامی است.";
                return false;
            }

            if (title.Length > MaxTitleLength)
            {
                error = $"عنوان حوزه فعالیت حداکثر {MaxTitleLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (text.Length > MaxTextLength)
            {
                error = $"توضیح حوزه فعالیت حداکثر {MaxTextLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (iconUrl.Length > MaxIconUrlLength)
            {
                error = $"آدرس آیکون حوزه فعالیت حداکثر {MaxIconUrlLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (iconUrl.Length > 0 && !IsValidUrlOrPath(iconUrl))
            {
                error = "آیکون حوزه فعالیت باید آدرس مطلق یا مسیر نسبی سایت باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<TeamSpecialtyPathItemDto> Normalize(IEnumerable<TeamSpecialtyPathItemDto> items)
    {
        var result = new List<TeamSpecialtyPathItemDto>();
        foreach (var item in items)
        {
            var title = (item.Title ?? string.Empty).Trim();
            var text = (item.Text ?? string.Empty).Trim();
            var iconUrl = string.IsNullOrWhiteSpace(item.IconUrl) ? null : item.IconUrl.Trim();
            if (title.Length == 0)
                continue;

            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];
            if (text.Length > MaxTextLength)
                text = text[..MaxTextLength];
            if (iconUrl is { Length: > MaxIconUrlLength })
                iconUrl = iconUrl[..MaxIconUrlLength];
            if (iconUrl is not null && !IsValidUrlOrPath(iconUrl))
                iconUrl = null;

            result.Add(new TeamSpecialtyPathItemDto(title, text, iconUrl));
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
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? IconUrl { get; set; }
    }
}
