using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Services.Application.ServiceItems;

public sealed record ServiceItemContentGuideItemDto(string Title, string Description);

public sealed record ServiceItemContentGuideDto(
    bool Enabled,
    string? Title,
    IReadOnlyList<ServiceItemContentGuideItemDto> Items);

public static class ServiceItemContentGuideJson
{
    public const int MaxItems = 50;
    public const int MaxTitleLength = 300;
    public const int MaxItemTitleLength = 300;
    public const int MaxItemDescriptionLength = 500;
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

    public static ServiceItemContentGuideDto Parse(string? contentGuideJson)
    {
        if (string.IsNullOrWhiteSpace(contentGuideJson))
            return Empty();

        try
        {
            var payload = JsonSerializer.Deserialize<ContentGuidePayload>(contentGuideJson, DeserializerOptions);
            if (payload is null)
                return Empty();

            return Normalize(new ServiceItemContentGuideDto(
                payload.Enabled,
                payload.Title,
                (payload.Items ?? [])
                    .Select(x => new ServiceItemContentGuideItemDto(x.Title ?? string.Empty, x.Description ?? string.Empty))
                    .ToList()));
        }
        catch (JsonException)
        {
            return Empty();
        }
    }

    public static string? Serialize(ServiceItemContentGuideDto? guide)
    {
        var normalized = Normalize(guide ?? Empty());
        if (!normalized.Enabled && string.IsNullOrWhiteSpace(normalized.Title) && normalized.Items.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            new ContentGuidePayload
            {
                Enabled = normalized.Enabled,
                Title = normalized.Title,
                Items = normalized.Items
                    .Select(x => new ContentGuideItemPayload { Title = x.Title, Description = x.Description })
                    .ToList()
            },
            SerializerOptions);
    }

    public static string? Serialize(bool enabled, string? title, IEnumerable<ServiceItemContentGuideItemDto>? items) =>
        Serialize(new ServiceItemContentGuideDto(enabled, title, (items ?? []).ToList()));

    public static string? NormalizeJson(string? contentGuideJson)
    {
        if (string.IsNullOrWhiteSpace(contentGuideJson))
            return null;

        return Serialize(Parse(contentGuideJson));
    }

    public static bool TryValidate(string? contentGuideJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(contentGuideJson))
            return true;

        if (contentGuideJson.Length > MaxJsonLength)
        {
            error = "راهنمای محتوای خدمت خیلی طولانی است.";
            return false;
        }

        ContentGuidePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ContentGuidePayload>(contentGuideJson, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت راهنمای محتوای خدمت نامعتبر است.";
            return false;
        }

        if (payload is null)
            return true;

        var title = (payload.Title ?? string.Empty).Trim();
        if (title.Length > MaxTitleLength)
        {
            error = $"عنوان راهنما حداکثر {MaxTitleLength} نویسه می‌تواند باشد.";
            return false;
        }

        var items = payload.Items ?? [];
        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} آیتم برای راهنمای محتوا مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var itemTitle = (item.Title ?? string.Empty).Trim();
            var description = (item.Description ?? string.Empty).Trim();
            if (itemTitle.Length == 0 && description.Length == 0)
                continue;
            if (itemTitle.Length == 0 || description.Length == 0)
            {
                error = "برای هر آیتم راهنما، هم عنوان و هم توضیح کوتاه الزامی است.";
                return false;
            }

            if (itemTitle.Length > MaxItemTitleLength)
            {
                error = $"عنوان آیتم حداکثر {MaxItemTitleLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (description.Length > MaxItemDescriptionLength)
            {
                error = $"توضیح کوتاه حداکثر {MaxItemDescriptionLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    public static bool ShouldRender(ServiceItemContentGuideDto guide) =>
        guide.Enabled && guide.Items.Count > 0;

    private static ServiceItemContentGuideDto Empty() =>
        new(false, null, []);

    private static ServiceItemContentGuideDto Normalize(ServiceItemContentGuideDto guide)
    {
        var title = string.IsNullOrWhiteSpace(guide.Title) ? null : guide.Title.Trim();
        if (title is { Length: > MaxTitleLength })
            title = title[..MaxTitleLength];

        var items = new List<ServiceItemContentGuideItemDto>();
        foreach (var item in guide.Items)
        {
            var itemTitle = (item.Title ?? string.Empty).Trim();
            var description = (item.Description ?? string.Empty).Trim();
            if (itemTitle.Length == 0 || description.Length == 0)
                continue;

            if (itemTitle.Length > MaxItemTitleLength)
                itemTitle = itemTitle[..MaxItemTitleLength];
            if (description.Length > MaxItemDescriptionLength)
                description = description[..MaxItemDescriptionLength];

            items.Add(new ServiceItemContentGuideItemDto(itemTitle, description));
            if (items.Count >= MaxItems)
                break;
        }

        return new ServiceItemContentGuideDto(guide.Enabled, title, items);
    }

    private sealed class ContentGuidePayload
    {
        public bool Enabled { get; set; }
        public string? Title { get; set; }
        public List<ContentGuideItemPayload>? Items { get; set; }
    }

    private sealed class ContentGuideItemPayload
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
