using System.Text.Json;

namespace CMS.Modules.Seo.Web;

public static class SeoSchemaBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        WriteIndented = false
    };

    public static string? BuildContentSchema(
        string? schemaType,
        string title,
        string? description,
        string? url,
        string? imageUrl,
        string? authorName,
        DateTime? datePublishedUtc,
        DateTime? dateModifiedUtc)
    {
        if (string.IsNullOrWhiteSpace(schemaType))
            return null;

        var type = schemaType.Trim();
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = type,
            ["headline"] = title,
            ["name"] = title
        };

        if (!string.IsNullOrWhiteSpace(description))
            payload["description"] = description.Trim();
        if (!string.IsNullOrWhiteSpace(url))
            payload["url"] = url.Trim();
        if (!string.IsNullOrWhiteSpace(imageUrl))
            payload["image"] = imageUrl.Trim();
        if (!string.IsNullOrWhiteSpace(authorName))
        {
            payload["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["name"] = authorName.Trim()
            };
        }

        if (datePublishedUtc.HasValue)
            payload["datePublished"] = datePublishedUtc.Value.ToString("O");
        if (dateModifiedUtc.HasValue)
            payload["dateModified"] = dateModifiedUtc.Value.ToString("O");

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
