using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;

namespace CMS.Modules.Forms.Application.Schema;

/// <summary>
/// Builds / parses FormVersion.SchemaJson. Fields may sync from FormDefinition.Fields;
/// submit/actions/antiSpam/settings are owned by schema.
/// </summary>
public static class FormSchemaLegacyMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new ObjectConfigJsonConverter() }
    };

    public static string ToSchemaJson(FormDefinition form) =>
        Serialize(ToDocument(form));

    public static string Serialize(FormSchemaDocument document) =>
        JsonSerializer.Serialize(document, JsonOptions);

    /// <summary>
    /// Builds a schema document from field rows with default empty submit/actions/antiSpam/settings.
    /// Does not read removed FormDefinition settings columns.
    /// </summary>
    public static FormSchemaDocument ToDocument(FormDefinition form)
    {
        var registry = new FormFieldTypeRegistry();
        var fields = form.Fields
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAtUtc)
            .Where(f => f.FieldType != FormFieldType.Captcha)
            .Select(f => FormFieldSchemaFactory.FromEntity(f, registry))
            .ToList();

        return new FormSchemaDocument
        {
            SchemaVersion = FormSchemaDocument.CurrentSchemaVersion,
            Fields = fields,
            SubmitBehavior = new FormSubmitBehaviorSchema { Type = FormSubmitBehaviorTypes.Message },
            Actions = [],
            AntiSpam = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Honeypot,
                Config = new Dictionary<string, object?>()
            },
            Settings = new FormSettingsSchema { SubmitButtonText = "ارسال" }
        };
    }

    public static FormSchemaDocument ApplySaveOverrides(
        FormSchemaDocument schema,
        FormDefinition form,
        SaveFormCommand command) =>
        ApplySaveOverrides(
            schema,
            form,
            command.SubmitBehaviorType,
            command.SuccessMessage,
            command.RedirectUrl,
            command.SubmitButtonText,
            command.SendEmailNotification,
            command.NotifyEmail,
            command.NotifyEmailSubject,
            command.NotifySenderName,
            ResolveFieldKey(form, command.NotifyReplyToFieldId) ?? command.NotifyReplyToFieldKey,
            command.AutoReplyEnabled,
            command.AutoReplySubject,
            command.AutoReplyBody,
            ResolveFieldKey(form, command.AutoReplyEmailFieldId) ?? command.AutoReplyEmailFieldKey,
            command.WebhookEnabled,
            command.WebhookUrl,
            command.WebhookSecret,
            command.AntiSpamEnabled,
            command.AntiSpamProvider,
            command.AntiSpamSiteKey,
            command.AntiSpamSecretKey,
            command.SimpleCaptchaExpected);

    public static FormSchemaDocument ApplySaveOverrides(
        FormSchemaDocument schema,
        FormDefinition form,
        string submitBehaviorType,
        string? successMessage,
        string? redirectUrl,
        string? submitButtonText,
        bool sendEmailNotification,
        string? notifyEmail,
        string? notifyEmailSubject,
        string? notifySenderName,
        string? notifyReplyToFieldKey,
        bool autoReplyEnabled,
        string? autoReplySubject,
        string? autoReplyBody,
        string? autoReplyEmailFieldKey,
        bool webhookEnabled,
        string? webhookUrl,
        string? webhookSecret = null,
        bool antiSpamEnabled = true,
        string? antiSpamProvider = null,
        string? antiSpamSiteKey = null,
        string? antiSpamSecretKey = null,
        string? simpleCaptchaExpected = null)
    {
        var behaviorType = string.IsNullOrWhiteSpace(submitBehaviorType)
            ? FormSubmitBehaviorTypes.Message
            : submitBehaviorType.Trim().ToLowerInvariant();

        var submitBehavior = behaviorType switch
        {
            FormSubmitBehaviorTypes.Redirect => new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Redirect,
                Url = redirectUrl,
                Message = successMessage
            },
            FormSubmitBehaviorTypes.Page => new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Page,
                Url = redirectUrl,
                Message = successMessage
            },
            _ => new FormSubmitBehaviorSchema
            {
                Type = FormSubmitBehaviorTypes.Message,
                Message = successMessage
            }
        };

        var actions = new List<FormActionSchema>();

        if (sendEmailNotification && !string.IsNullOrWhiteSpace(notifyEmail))
        {
            actions.Add(new FormActionSchema
            {
                Id = $"action_notify_{form.Id:N}"[..24],
                Type = FormActionTypeIds.EmailNotification,
                Enabled = true,
                Config = new Dictionary<string, object?>
                {
                    ["to"] = new[] { notifyEmail.Trim() },
                    ["subject"] = notifyEmailSubject ?? $"پاسخ جدید فرم — {form.Name}",
                    ["senderName"] = notifySenderName,
                    ["replyToFieldKey"] = string.IsNullOrWhiteSpace(notifyReplyToFieldKey)
                        ? null
                        : notifyReplyToFieldKey.Trim().ToLowerInvariant(),
                    ["replyToFieldId"] = ResolveFieldId(schema.Fields, notifyReplyToFieldKey)
                }
            });
        }

        if (autoReplyEnabled)
        {
            actions.Add(new FormActionSchema
            {
                Id = $"action_reply_{form.Id:N}"[..24],
                Type = FormActionTypeIds.AutoReply,
                Enabled = true,
                Config = new Dictionary<string, object?>
                {
                    ["subject"] = autoReplySubject,
                    ["body"] = autoReplyBody,
                    ["recipientFieldKey"] = string.IsNullOrWhiteSpace(autoReplyEmailFieldKey)
                        ? null
                        : autoReplyEmailFieldKey.Trim().ToLowerInvariant(),
                    ["recipientFieldId"] = ResolveFieldId(schema.Fields, autoReplyEmailFieldKey)
                }
            });
        }

        if (webhookEnabled && !string.IsNullOrWhiteSpace(webhookUrl))
        {
            var webhookConfig = new Dictionary<string, object?>
            {
                ["url"] = webhookUrl.Trim()
            };
            if (!string.IsNullOrWhiteSpace(webhookSecret))
                webhookConfig["secretKey"] = webhookSecret.Trim();

            actions.Add(new FormActionSchema
            {
                Id = $"action_webhook_{form.Id:N}"[..24],
                Type = FormActionTypeIds.Webhook,
                Enabled = true,
                Config = webhookConfig
            });
        }

        var provider = string.IsNullOrWhiteSpace(antiSpamProvider)
            ? FormAntiSpamProviderIds.Honeypot
            : antiSpamProvider.Trim().ToLowerInvariant();

        // siteKey / secretKey are global (Forms:AntiSpam env); do not persist per form.
        _ = antiSpamSiteKey;
        _ = antiSpamSecretKey;

        var config = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(simpleCaptchaExpected))
            config["expected"] = simpleCaptchaExpected.Trim();
        if (provider == FormAntiSpamProviderIds.SimpleCaptcha)
            config["legacyEnableCaptcha"] = true;

        var antiSpam = new FormAntiSpamSchema
        {
            Enabled = antiSpamEnabled,
            Provider = provider,
            Config = config
        };

        var button = string.IsNullOrWhiteSpace(submitButtonText) ? "ارسال" : submitButtonText.Trim();

        return new FormSchemaDocument
        {
            SchemaVersion = schema.SchemaVersion,
            Fields = schema.Fields,
            SubmitBehavior = submitBehavior,
            Actions = actions,
            AntiSpam = antiSpam,
            Settings = new FormSettingsSchema { SubmitButtonText = button }
        };
    }

    public static FormSchemaDocument? TryParse(string? schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<FormSchemaDocument>(schemaJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ResolveFieldId(IReadOnlyList<FormSchemaField> fields, string? fieldKey)
    {
        if (string.IsNullOrWhiteSpace(fieldKey))
            return null;

        return fields.FirstOrDefault(f =>
                f.Key.Equals(fieldKey.Trim(), StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private static string? ResolveFieldKey(FormDefinition form, Guid? fieldId)
    {
        if (fieldId is null)
            return null;

        return form.Fields.FirstOrDefault(f => f.Id == fieldId.Value)?.Key;
    }

    /// <summary>
    /// Allows re-serializing action/antiSpam config dictionaries that contain JsonElement values
    /// after a deserialize round-trip of <c>Dictionary&lt;string, object?&gt;</c>.
    /// </summary>
    private sealed class ObjectConfigJsonConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return doc.RootElement.Clone();
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

            if (value is JsonElement el)
            {
                el.WriteTo(writer);
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
