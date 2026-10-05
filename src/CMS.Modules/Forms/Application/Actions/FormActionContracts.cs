using System.Net;
using System.Text.RegularExpressions;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Entities;

namespace CMS.Modules.Forms.Application.Actions;

public sealed class FormActionExecutionContext
{
    public required FormDefinition Form { get; init; }
    public required FormSubmission Submission { get; init; }
    public required FormSchemaDocument Schema { get; init; }
    public required IReadOnlyDictionary<string, string?> FieldValuesByKey { get; init; }
    public required IReadOnlyDictionary<string, string?> FieldValuesById { get; init; }
    public string? SiteName { get; init; }
    public string? PageUrl { get; init; }
}

public interface IFormActionHandler
{
    string TypeId { get; }
    Task ExecuteAsync(FormActionSchema action, FormActionExecutionContext context, CancellationToken cancellationToken);
}

public interface IFormActionExecutor
{
    Task ExecuteAllAsync(FormActionExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders {{placeholders}} and legacy {placeholders} with optional HTML encoding.
/// </summary>
public static class FormTemplateRenderer
{
    private static readonly Regex PlaceholderPattern = new(
        @"\{\{([^{}]+)\}\}|\{([^{}]+)\}",
        RegexOptions.Compiled);

    public static string Render(
        string? template,
        FormActionExecutionContext context,
        bool htmlEncode = true)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return PlaceholderPattern.Replace(template, match =>
        {
            var token = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Trim();
            var value = Resolve(token, context) ?? string.Empty;
            return htmlEncode ? WebUtility.HtmlEncode(value) : value;
        });
    }

    private static string? Resolve(string token, FormActionExecutionContext context)
    {
        if (token.Equals("form.title", StringComparison.OrdinalIgnoreCase)
            || token.Equals("form.name", StringComparison.OrdinalIgnoreCase))
            return context.Form.Name;

        if (token.Equals("form.key", StringComparison.OrdinalIgnoreCase))
            return context.Form.Key;

        if (token.Equals("form.slug", StringComparison.OrdinalIgnoreCase))
            return context.Form.Slug;

        if (token.Equals("submission.id", StringComparison.OrdinalIgnoreCase))
            return context.Submission.Id.ToString("D");

        if (token.Equals("submission.createdAt", StringComparison.OrdinalIgnoreCase)
            || token.Equals("submission.submittedAt", StringComparison.OrdinalIgnoreCase))
            return JalaliDateHelper.FormatFromUtc(context.Submission.SubmittedAtUtc);

        if (token.Equals("site.name", StringComparison.OrdinalIgnoreCase))
            return context.SiteName;

        if (token.Equals("page.url", StringComparison.OrdinalIgnoreCase))
            return context.PageUrl;

        if (context.FieldValuesByKey.TryGetValue(token, out var byKey))
            return byKey;

        if (context.FieldValuesById.TryGetValue(token, out var byId))
            return byId;

        // Common aliases
        if (token.Equals("full_name", StringComparison.OrdinalIgnoreCase)
            && context.FieldValuesByKey.TryGetValue("name", out var name))
            return name;

        return null;
    }
}

public static class FormActionConfigReader
{
    public static string? GetString(IReadOnlyDictionary<string, object?> config, string key)
    {
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return null;

        return raw switch
        {
            string s => s,
            System.Text.Json.JsonElement el when el.ValueKind == System.Text.Json.JsonValueKind.String => el.GetString(),
            System.Text.Json.JsonElement el => el.ToString(),
            _ => raw.ToString()
        };
    }

    public static IReadOnlyList<string> GetStringList(IReadOnlyDictionary<string, object?> config, string key)
    {
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return [];

        if (raw is string s)
            return string.IsNullOrWhiteSpace(s) ? [] : [s.Trim()];

        if (raw is IEnumerable<object?> objs)
            return objs.Select(o => o?.ToString()?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();

        if (raw is System.Text.Json.JsonElement el)
        {
            if (el.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = el.GetString();
                return string.IsNullOrWhiteSpace(value) ? [] : [value.Trim()];
            }

            if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                return el.EnumerateArray()
                    .Select(x => x.GetString()?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .ToList();
            }
        }

        var single = raw.ToString();
        return string.IsNullOrWhiteSpace(single) ? [] : [single.Trim()];
    }

    public static bool GetBool(IReadOnlyDictionary<string, object?> config, string key, bool defaultValue = false)
    {
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return defaultValue;

        if (raw is bool b)
            return b;

        if (raw is System.Text.Json.JsonElement el)
        {
            if (el.ValueKind is System.Text.Json.JsonValueKind.True)
                return true;
            if (el.ValueKind is System.Text.Json.JsonValueKind.False)
                return false;
            if (el.ValueKind == System.Text.Json.JsonValueKind.String
                && bool.TryParse(el.GetString(), out var parsed))
                return parsed;
        }

        return bool.TryParse(raw.ToString(), out var result) ? result : defaultValue;
    }
}

public static class FormSubmitBehaviorResolver
{
    public static (string? SuccessMessage, string? RedirectUrl) Resolve(FormSchemaDocument? schema) =>
        Resolve(schema, legacySuccessMessage: null, legacyRedirectUrl: null);

    public static (string? SuccessMessage, string? RedirectUrl) Resolve(
        FormSchemaDocument? schema,
        string? legacySuccessMessage,
        string? legacyRedirectUrl)
    {
        if (schema?.SubmitBehavior is { } behavior && !string.IsNullOrWhiteSpace(behavior.Type))
        {
            return behavior.Type.Trim().ToLowerInvariant() switch
            {
                FormSubmitBehaviorTypes.Redirect => (behavior.Message, behavior.Url),
                FormSubmitBehaviorTypes.Page => (behavior.Message, behavior.Url),
                _ => (behavior.Message, null)
            };
        }

        return (legacySuccessMessage, legacyRedirectUrl);
    }
}
