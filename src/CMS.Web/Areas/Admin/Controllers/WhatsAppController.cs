using CMS.Application.Audit;
using CMS.Application.Messaging;
using CMS.Application.WhatsApp;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Auth;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class WhatsAppController : Controller
{
    private readonly IWhatsAppGateway _gateway;
    private readonly IWhatsAppSettingsService _settings;
    private readonly IMessageLogQueryService _messageLogs;
    private readonly IMessageLogger _messageLogger;
    private readonly WhatsAppOptions _options;
    private readonly IAuditLogger _audit;
    private readonly IStringLocalizer<AdminShared> _localizer;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IWhatsAppGateway gateway,
        IWhatsAppSettingsService settings,
        IMessageLogQueryService messageLogs,
        IMessageLogger messageLogger,
        IOptions<WhatsAppOptions> options,
        IAuditLogger audit,
        IStringLocalizer<AdminShared> localizer,
        ILogger<WhatsAppController> logger)
    {
        _gateway = gateway;
        _settings = settings;
        _messageLogs = messageLogs;
        _messageLogger = messageLogger;
        _options = options.Value;
        _audit = audit;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["WhatsApp"].Value;
        var page = await BuildPageAsync(cancellationToken);
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        [Bind(Prefix = "Settings")] WhatsAppSettingsFormViewModel model,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["WhatsApp"].Value;
        if (!ModelState.IsValid)
        {
            var invalidPage = await BuildPageAsync(cancellationToken);
            return View(new WhatsAppSettingsPageViewModel
            {
                Settings = model,
                Status = invalidPage.Status,
                Groups = invalidPage.Groups,
                RecentLogs = invalidPage.RecentLogs,
                ServiceWarning = invalidPage.ServiceWarning
            });
        }

        try
        {
            await PersistSettingsAsync(model, cancellationToken, preserveTemplateIfEmpty: false);
            TempData["Success"] = _localizer["WhatsAppSettingsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(await BuildPageAsync(cancellationToken));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(await BuildPageAsync(cancellationToken));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(
        [Bind(Prefix = "Settings")] WhatsAppSettingsFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return Json(new { ok = false, error = "اطلاعات نامعتبر است." });

        try
        {
            await PersistSettingsAsync(model, cancellationToken, preserveTemplateIfEmpty: true);
            return Json(new
            {
                ok = true,
                defaultGroupId = model.DefaultGroupId,
                defaultGroupName = model.DefaultGroupName
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp settings AJAX save failed");
            return Json(new { ok = false, error = ex.Message });
        }
    }

    private async Task PersistSettingsAsync(
        WhatsAppSettingsFormViewModel model,
        CancellationToken cancellationToken,
        bool preserveTemplateIfEmpty)
    {
        var groupId = string.IsNullOrWhiteSpace(model.DefaultGroupId) ? null : model.DefaultGroupId.Trim();
        var groupName = string.IsNullOrWhiteSpace(model.DefaultGroupName) ? null : model.DefaultGroupName.Trim();

        if (!string.IsNullOrWhiteSpace(groupId) && string.IsNullOrWhiteSpace(groupName))
        {
            try
            {
                var groups = await _gateway.ListGroupsAsync(cancellationToken);
                groupName = groups.FirstOrDefault(g =>
                        string.Equals(g.Id, groupId, StringComparison.OrdinalIgnoreCase))
                    ?.Name;
            }
            catch
            {
                // keep name empty when service unavailable
            }
        }

        if (preserveTemplateIfEmpty && string.IsNullOrWhiteSpace(model.DefaultTemplate))
        {
            var current = await _settings.GetAsync(cancellationToken);
            model.DefaultTemplate = current.DefaultTemplate;
        }

        model.DefaultGroupId = groupId;
        model.DefaultGroupName = groupName;

        await _settings.UpdateAsync(
            new UpdateWhatsAppSettingsCommand(
                model.Enabled,
                groupId,
                groupName,
                model.DefaultTemplate),
            cancellationToken);

        await _audit.LogAsync(
            "Update",
            "WhatsAppSettings",
            details: model.Enabled ? "enabled" : "disabled",
            cancellationToken: cancellationToken);
    }

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.IsConfigured)
                return Json(new { ok = false, error = "not_configured", status = WhatsAppConnectionStatuses.Disconnected });

            var status = await _gateway.GetStatusAsync(cancellationToken);
            var settings = await _settings.GetAsync(cancellationToken);
            return Json(new
            {
                ok = true,
                status = status.Status,
                phoneNumber = status.PhoneNumber,
                pushName = status.PushName,
                lastConnectedAtUtc = status.LastConnectedAtUtc,
                lastSuccessfulSendAtUtc = settings.LastSuccessfulSendAtUtc,
                lastError = status.LastError,
                hasQr = status.HasQr,
                connected = status.Connected
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp status poll failed");
            return Json(new { ok = false, error = ex.Message, status = WhatsAppConnectionStatuses.Disconnected });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Qr(CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.IsConfigured)
                return Json(new { ok = false, error = "not_configured" });

            var qr = await _gateway.GetQrAsync(cancellationToken);
            return Json(new
            {
                ok = true,
                status = qr.Status,
                qrDataUrl = qr.QrDataUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp QR poll failed");
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Groups(CancellationToken cancellationToken)
    {
        try
        {
            var groups = await _gateway.ListGroupsAsync(cancellationToken);
            return Json(new { ok = true, groups });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp groups list failed");
            return Json(new { ok = false, error = ex.Message, groups = Array.Empty<object>() });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reconnect(CancellationToken cancellationToken) =>
        RunActionAsync(() => _gateway.ReconnectAsync(cancellationToken), "Reconnect", cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Logout(CancellationToken cancellationToken) =>
        RunActionAsync(() => _gateway.LogoutAsync(cancellationToken), "Logout", cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ClearSession(CancellationToken cancellationToken) =>
        RunActionAsync(() => _gateway.ClearSessionAsync(cancellationToken), "ClearSession", cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RegenerateQr(CancellationToken cancellationToken) =>
        RunActionAsync(() => _gateway.RegenerateQrAsync(cancellationToken), "RegenerateQr", cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestMessage(
        [FromBody] WhatsAppTestMessageRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return Json(new { ok = false, error = "متن پیام الزامی است." });

        var settings = await _settings.GetAsync(cancellationToken);
        var groupId = string.IsNullOrWhiteSpace(request.GroupId)
            ? settings.DefaultGroupId
            : request.GroupId.Trim();

        if (string.IsNullOrWhiteSpace(groupId))
            return Json(new { ok = false, error = "گروه مقصد انتخاب نشده است." });

        // Test bypasses the global "Enabled" switch so admins can verify connectivity first.
        var result = await _gateway.SendToGroupAsync(groupId, request.Message.Trim(), cancellationToken);
        var status = result.Skipped
            ? MessageSendStatus.Skipped
            : result.Succeeded
                ? MessageSendStatus.Succeeded
                : MessageSendStatus.Failed;

        var recipient = string.IsNullOrWhiteSpace(settings.DefaultGroupName)
            ? groupId
            : $"{settings.DefaultGroupName} ({groupId})";

        await _messageLogger.LogWhatsAppAsync(
            recipient,
            "تست پنل",
            request.Message.Trim(),
            status,
            result.MessageId,
            result.ErrorMessage,
            cancellationToken);

        if (result.Succeeded)
            await _settings.MarkSuccessfulSendAsync(cancellationToken);

        return Json(new
        {
            ok = result.Succeeded,
            skipped = result.Skipped,
            error = result.ErrorMessage,
            messageId = result.MessageId
        });
    }

    [HttpGet]
    public async Task<IActionResult> LogDetail(Guid id, CancellationToken cancellationToken)
    {
        var logs = await _messageLogs.ListRecentAsync(500, cancellationToken);
        var log = logs.FirstOrDefault(x => x.Id == id && x.Channel == MessageChannel.WhatsApp);
        if (log is null)
            return NotFound();

        return Json(new
        {
            ok = true,
            id = log.Id,
            status = log.Status.ToString(),
            recipient = log.Recipient,
            formName = log.Subject,
            bodyPreview = log.BodyPreview,
            errorMessage = log.ErrorMessage,
            providerMessageId = log.ProviderMessageId,
            createdAtUtc = log.CreatedAtUtc
        });
    }

    private async Task<IActionResult> RunActionAsync(
        Func<Task<WhatsAppStatusDto>> action,
        string auditAction,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await action();
            await _audit.LogAsync(auditAction, "WhatsApp", details: status.Status, cancellationToken: cancellationToken);
            return Json(new
            {
                ok = true,
                status = status.Status,
                phoneNumber = status.PhoneNumber,
                connected = status.Connected,
                hasQr = status.HasQr,
                lastError = status.LastError
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WhatsApp admin action {Action} failed", auditAction);
            return Json(new { ok = false, error = ex.Message });
        }
    }

    private async Task<WhatsAppSettingsPageViewModel> BuildPageAsync(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAsync(cancellationToken);
        string? warning = null;
        WhatsAppStatusDto status;
        IReadOnlyList<WhatsAppGroupDto> groups = [];

        if (!_options.IsConfigured)
        {
            warning = "سرویس واتساپ پیکربندی نشده است. متغیرهای WhatsApp__ServiceBaseUrl و WhatsApp__ApiKey را در .env تنظیم کنید.";
            status = new WhatsAppStatusDto(
                WhatsAppConnectionStatuses.Disconnected,
                null,
                null,
                null,
                warning,
                false,
                false);
        }
        else
        {
            try
            {
                status = await _gateway.GetStatusAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                warning = "ارتباط با سرویس واتساپ برقرار نشد.";
                status = new WhatsAppStatusDto(
                    WhatsAppConnectionStatuses.Disconnected,
                    null,
                    null,
                    null,
                    ex.Message,
                    false,
                    false);
            }

            if (status.Connected)
            {
                try
                {
                    groups = await _gateway.ListGroupsAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load WhatsApp groups for settings page");
                }
            }
        }

        // Keep the saved group visible even if live list is temporarily empty.
        if (!string.IsNullOrWhiteSpace(settings.DefaultGroupId)
            && !groups.Any(g => string.Equals(g.Id, settings.DefaultGroupId, StringComparison.OrdinalIgnoreCase)))
        {
            groups = groups
                .Prepend(new WhatsAppGroupDto(
                    settings.DefaultGroupId!,
                    string.IsNullOrWhiteSpace(settings.DefaultGroupName)
                        ? settings.DefaultGroupId!
                        : settings.DefaultGroupName!,
                    null))
                .ToList();
        }

        var recent = (await _messageLogs.ListRecentAsync(100, cancellationToken))
            .Where(x => x.Channel == MessageChannel.WhatsApp)
            .Take(30)
            .ToList();

        return new WhatsAppSettingsPageViewModel
        {
            Settings = new WhatsAppSettingsFormViewModel
            {
                Enabled = settings.Enabled,
                DefaultGroupId = settings.DefaultGroupId,
                DefaultGroupName = settings.DefaultGroupName,
                DefaultTemplate = settings.DefaultTemplate,
                LastSuccessfulSendAtUtc = settings.LastSuccessfulSendAtUtc
            },
            Status = status,
            Groups = groups,
            RecentLogs = recent,
            ServiceWarning = warning
        };
    }
}
