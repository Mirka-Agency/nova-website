using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Blog.Application.Posts;

/// <summary>Ordered related post ids stored as JSON array of GUID strings.</summary>
public static class PostRelatedPostsJson
{
    public const int MaxItems = 6;
    public const int MaxJsonLength = 4_000;

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

    public static IReadOnlyList<Guid> Parse(string? relatedPostsJson)
    {
        if (string.IsNullOrWhiteSpace(relatedPostsJson))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(relatedPostsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<Guid>();
            var seen = new HashSet<Guid>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!TryReadId(el, out var id) || !seen.Add(id))
                    continue;
                result.Add(id);
                if (result.Count >= MaxItems)
                    break;
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<Guid>? ids)
    {
        var normalized = Normalize(ids ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(normalized.Select(x => x.ToString("D")), SerializerOptions);
    }

    public static string? NormalizeJson(string? relatedPostsJson)
    {
        if (string.IsNullOrWhiteSpace(relatedPostsJson))
            return null;

        return Serialize(Parse(relatedPostsJson));
    }

    public static bool TryValidate(string? relatedPostsJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(relatedPostsJson))
            return true;

        if (relatedPostsJson.Length > MaxJsonLength)
        {
            error = "لیست مطالب مرتبط خیلی طولانی است.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(relatedPostsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                error = "فرمت مطالب مرتبط نامعتبر است.";
                return false;
            }

            var count = 0;
            var seen = new HashSet<Guid>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!TryReadId(el, out var id))
                {
                    error = "شناسه مطلب مرتبط نامعتبر است.";
                    return false;
                }

                if (!seen.Add(id))
                    continue;

                count++;
                if (count > MaxItems)
                {
                    error = $"حداکثر {MaxItems} مطلب مرتبط مجاز است.";
                    return false;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            error = "فرمت مطالب مرتبط نامعتبر است.";
            return false;
        }
    }

    private static IReadOnlyList<Guid> Normalize(IEnumerable<Guid> ids)
    {
        var result = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var id in ids)
        {
            if (id == Guid.Empty || !seen.Add(id))
                continue;
            result.Add(id);
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private static bool TryReadId(JsonElement el, out Guid id)
    {
        id = Guid.Empty;
        if (el.ValueKind == JsonValueKind.String)
            return Guid.TryParse(el.GetString(), out id) && id != Guid.Empty;

        if (el.ValueKind == JsonValueKind.Object
            && el.TryGetProperty("id", out var idProp)
            && idProp.ValueKind == JsonValueKind.String)
            return Guid.TryParse(idProp.GetString(), out id) && id != Guid.Empty;

        return false;
    }
}
