using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Entities;

namespace CMS.Modules.Forms.Application.Actions;

public sealed class FormWebhookPayload
{
    [JsonPropertyName("event")]
    public string Event { get; init; } = "form.submission.created";

    [JsonPropertyName("form")]
    public required FormWebhookForm Form { get; init; }

    [JsonPropertyName("submission")]
    public required FormWebhookSubmission Submission { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("context")]
    public object? Context { get; init; }

    public static FormWebhookPayload From(FormActionExecutionContext context)
    {
        object? data = null;
        if (!string.IsNullOrWhiteSpace(context.Submission.DataJson))
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(context.Submission.DataJson);
            }
            catch (JsonException)
            {
                data = context.Submission.DataJson;
            }
        }

        object? ctx = null;
        if (!string.IsNullOrWhiteSpace(context.Submission.ContextJson))
        {
            try
            {
                ctx = JsonSerializer.Deserialize<JsonElement>(context.Submission.ContextJson);
            }
            catch (JsonException)
            {
                ctx = context.Submission.ContextJson;
            }
        }

        return new FormWebhookPayload
        {
            Form = new FormWebhookForm
            {
                Id = context.Form.Id,
                Key = context.Form.Key,
                Slug = context.Form.Slug,
                Name = context.Form.Name
            },
            Submission = new FormWebhookSubmission
            {
                Id = context.Submission.Id,
                FormVersionId = context.Submission.FormVersionId,
                CreatedAtUtc = context.Submission.CreatedAtUtc
            },
            Data = data,
            Context = ctx
        };
    }

    public static string ComputeSignatureHex(string body, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class FormWebhookForm
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
}

public sealed class FormWebhookSubmission
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("formVersionId")]
    public Guid FormVersionId { get; init; }

    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; init; }
}
