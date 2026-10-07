using System.Net.Mail;
using System.Text;
using System.Text.Json;
using CMS.Application.Email;
using CMS.Application.WhatsApp;
using CMS.Modules.Forms.Application.Common;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Security;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Application.Actions;

public sealed class EmailNotificationActionHandler : IFormActionHandler
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationActionHandler> _logger;

    public EmailNotificationActionHandler(
        IEmailSender emailSender,
        ILogger<EmailNotificationActionHandler> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public string TypeId => FormActionTypeIds.EmailNotification;

    public async Task ExecuteAsync(
        FormActionSchema action,
        FormActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        var toList = FormActionConfigReader.GetStringList(action.Config, "to");
        if (toList.Count == 0)
        {
            _logger.LogWarning("email_notification action {ActionId} has no recipients", action.Id);
            return;
        }

        var subjectTemplate = FormActionConfigReader.GetString(action.Config, "subject")
                              ?? $"پاسخ جدید فرم — {context.Form.Name}";
        var bodyTemplate = FormActionConfigReader.GetString(action.Config, "body");
        var senderName = FormActionConfigReader.GetString(action.Config, "senderName");
        if (string.IsNullOrWhiteSpace(senderName))
            senderName = FormTemplateRenderer.Render(
                FormActionConfigReader.GetString(action.Config, "senderNameTemplate") ?? "{{site.name}}",
                context,
                htmlEncode: false);

        var replyTo = ResolveFieldValue(
            context,
            FormActionConfigReader.GetString(action.Config, "replyToFieldId"),
            FormActionConfigReader.GetString(action.Config, "replyToFieldKey"));

        if (!string.IsNullOrWhiteSpace(replyTo) && !IsValidEmail(replyTo))
            replyTo = null;

        var subject = FormTemplateRenderer.Render(subjectTemplate, context, htmlEncode: false);
        string body;
        if (!string.IsNullOrWhiteSpace(bodyTemplate))
        {
            body = FormTemplateRenderer.Render(bodyTemplate, context, htmlEncode: true);
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(WebUtilityEncode($"فرم جدید: {context.Form.Name}"));
            sb.AppendLine(WebUtilityEncode($"زمان: {JalaliDateHelper.FormatFromUtc(context.Submission.SubmittedAtUtc)}"));
            sb.AppendLine();
            foreach (var (key, value) in context.FieldValuesByKey)
                sb.AppendLine(WebUtilityEncode($"{key}: {value ?? "—"}"));
            body = sb.ToString();
        }

        foreach (var to in toList.Where(IsValidEmail))
        {
            await _emailSender.SendAsync(
                new EmailMessage(
                    to,
                    subject,
                    body,
                    IsHtml: !string.IsNullOrWhiteSpace(bodyTemplate),
                    ReplyTo: replyTo,
                    FromDisplayName: string.IsNullOrWhiteSpace(senderName) ? null : senderName),
                cancellationToken);
        }
    }

    private static string? ResolveFieldValue(
        FormActionExecutionContext context,
        string? fieldId,
        string? fieldKey)
    {
        if (!string.IsNullOrWhiteSpace(fieldId)
            && context.FieldValuesById.TryGetValue(fieldId.Trim(), out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(fieldKey)
            && context.FieldValuesByKey.TryGetValue(fieldKey.Trim(), out var byKey))
            return byKey;

        return null;
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return value.Contains('@');
        }
        catch
        {
            return false;
        }
    }

    private static string WebUtilityEncode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}

public sealed class AutoReplyActionHandler : IFormActionHandler
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AutoReplyActionHandler> _logger;

    public AutoReplyActionHandler(IEmailSender emailSender, ILogger<AutoReplyActionHandler> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public string TypeId => FormActionTypeIds.AutoReply;

    public async Task ExecuteAsync(
        FormActionSchema action,
        FormActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        var to = ResolveFieldValue(
            context,
            FormActionConfigReader.GetString(action.Config, "recipientFieldId"),
            FormActionConfigReader.GetString(action.Config, "recipientFieldKey"));

        if (string.IsNullOrWhiteSpace(to) || !IsValidEmail(to))
        {
            _logger.LogWarning("auto_reply action {ActionId} missing valid recipient", action.Id);
            return;
        }

        var subjectTemplate = FormActionConfigReader.GetString(action.Config, "subject");
        var bodyTemplate = FormActionConfigReader.GetString(action.Config, "body");
        if (string.IsNullOrWhiteSpace(subjectTemplate) || string.IsNullOrWhiteSpace(bodyTemplate))
        {
            _logger.LogWarning("auto_reply action {ActionId} missing subject/body", action.Id);
            return;
        }

        var subject = FormTemplateRenderer.Render(subjectTemplate, context, htmlEncode: false);
        var body = FormTemplateRenderer.Render(bodyTemplate, context, htmlEncode: true);

        await _emailSender.SendAsync(
            new EmailMessage(to, subject, body, IsHtml: true),
            cancellationToken);
    }

    private static string? ResolveFieldValue(
        FormActionExecutionContext context,
        string? fieldId,
        string? fieldKey)
    {
        if (!string.IsNullOrWhiteSpace(fieldId)
            && context.FieldValuesById.TryGetValue(fieldId.Trim(), out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(fieldKey)
            && context.FieldValuesByKey.TryGetValue(fieldKey.Trim(), out var byKey))
            return byKey;

        return null;
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return value.Contains('@');
        }
        catch
        {
            return false;
        }
    }
}

public sealed class WebhookActionHandler : IFormActionHandler
{
    private readonly ILogger<WebhookActionHandler> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;

    public WebhookActionHandler(
        ILogger<WebhookActionHandler> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string TypeId => FormActionTypeIds.Webhook;

    public async Task ExecuteAsync(
        FormActionSchema action,
        FormActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        var url = FormActionConfigReader.GetString(action.Config, "url");
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogInformation(
                "webhook action {ActionId} for form {FormKey} skipped (no url configured)",
                action.Id,
                context.Form.Key);
            return;
        }

        if (!WebhookUrlGuard.TryValidatePublicHttpsUrl(url, out var urlError))
        {
            _logger.LogWarning(
                "webhook action {ActionId} blocked: {Reason}",
                action.Id,
                urlError);
            return;
        }

        if (_httpClientFactory is null)
        {
            _logger.LogWarning(
                "webhook action {ActionId}: HttpClient unavailable; skipping POST to {Url}",
                action.Id,
                url);
            return;
        }

        try
        {
            var payload = FormWebhookPayload.From(context);
            var body = JsonSerializer.Serialize(payload);
            var client = _httpClientFactory.CreateClient("forms-webhook");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("User-Agent", "MirkaForms/1.0");
            request.Headers.TryAddWithoutValidation("X-Form-Delivery-Id", context.Submission.Id.ToString("D"));

            var secret = FormActionConfigReader.GetString(action.Config, "secretKey")
                         ?? FormActionConfigReader.GetString(action.Config, "secret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                var signature = FormWebhookPayload.ComputeSignatureHex(body, secret);
                request.Headers.TryAddWithoutValidation("X-Form-Signature", $"sha256={signature}");
            }

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "webhook action {ActionId} returned {StatusCode}",
                    action.Id,
                    (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "webhook action {ActionId} failed", action.Id);
        }
    }
}

public sealed class WhatsAppNotificationActionHandler : IFormActionHandler
{
    private readonly IWhatsAppNotifier _whatsApp;
    private readonly IWhatsAppSettingsService _settings;
    private readonly ILogger<WhatsAppNotificationActionHandler> _logger;

    public WhatsAppNotificationActionHandler(
        IWhatsAppNotifier whatsApp,
        IWhatsAppSettingsService settings,
        ILogger<WhatsAppNotificationActionHandler> logger)
    {
        _whatsApp = whatsApp;
        _settings = settings;
        _logger = logger;
    }

    public string TypeId => FormActionTypeIds.WhatsAppNotification;

    public async Task ExecuteAsync(
        FormActionSchema action,
        FormActionExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var global = await _settings.GetAsync(cancellationToken);
            if (!global.Enabled)
            {
                _logger.LogInformation(
                    "whatsapp_notification skipped for form {FormKey}: globally disabled",
                    context.Form.Key);
                return;
            }

            var groupId = FormActionConfigReader.GetString(action.Config, "groupId");
            var groupName = FormActionConfigReader.GetString(action.Config, "groupName");
            var template = FormActionConfigReader.GetString(action.Config, "template");

            if (string.IsNullOrWhiteSpace(template))
                template = global.DefaultTemplate;
            if (string.IsNullOrWhiteSpace(template))
                template = WhatsAppDefaultTemplate.Value;

            var message = FormTemplateRenderer.Render(template, context, htmlEncode: false);
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning(
                    "whatsapp_notification action {ActionId} produced empty message",
                    action.Id);
                return;
            }

            var result = await _whatsApp.NotifyGroupAsync(
                groupId,
                groupName,
                message,
                context.Form.Name,
                cancellationToken);

            if (!result.Succeeded && !result.Skipped)
            {
                _logger.LogWarning(
                    "whatsapp_notification action {ActionId} failed: {Error}",
                    action.Id,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            // Never fail the form submission pipeline because of WhatsApp.
            _logger.LogError(ex, "whatsapp_notification action {ActionId} threw", action.Id);
        }
    }
}
