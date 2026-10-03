using System.Globalization;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Web.Areas.Admin.ViewModels;
using CMS.Modules.Forms.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Forms.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewForms")]
public class FormSubmissionsController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();
    private static readonly Regex JalaliDateRegex = new(
        @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ISubmissionService _submissions;
    private readonly IFormService _forms;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<FormsAdmin> _localizer;

    public FormSubmissionsController(
        ISubmissionService submissions,
        IFormService forms,
        IFeatureManager features,
        IStringLocalizer<FormsAdmin> localizer)
    {
        _submissions = submissions;
        _forms = forms;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        Guid? formId,
        int page = 1,
        string? q = null,
        SubmissionStatus? status = null,
        string? from = null,
        string? to = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        ViewData["Title"] = status == SubmissionStatus.Archived
            ? _localizer["SubmissionStatus_Archived"].Value
            : _localizer["Submissions"].Value;
        string? formName = null;
        if (formId.HasValue)
        {
            var form = await _forms.GetAsync(formId.Value, cancellationToken);
            formName = form?.Name;
        }

        var fromLocal = NormalizeDateInput(from);
        var toLocal = NormalizeDateInput(to);
        var fromUtc = ToUtc(fromLocal, endOfDay: false);
        var toUtc = ToUtc(toLocal, endOfDay: true);

        var result = await _submissions.ListPagedAsync(
            new SubmissionListRequest
            {
                FormId = formId,
                Page = page,
                Search = q,
                Status = status,
                FromUtc = fromUtc,
                ToUtc = toUtc
            },
            cancellationToken);

        var model = new SubmissionIndexViewModel
        {
            FormId = formId,
            FormName = formName,
            Search = q,
            Status = status,
            FromLocal = fromLocal,
            ToLocal = toLocal,
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(s => new SubmissionListItemViewModel
            {
                Id = s.Id,
                FormId = s.FormId,
                FormName = s.FormName,
                SubmittedAtUtc = s.SubmittedAtUtc,
                Status = s.Status,
                StatusLabel = StatusLabel(s.Status),
                Preview = s.Preview,
                IpAddress = s.IpAddress
            }).ToList()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var submission = await _submissions.GetAsync(id, cancellationToken);
        if (submission is null)
            return NotFound();

        if (submission.Status == SubmissionStatus.New)
        {
            try
            {
                await _submissions.MarkReadAsync(id, cancellationToken);
                submission = await _submissions.GetAsync(id, cancellationToken) ?? submission;
            }
            catch
            {
                // Viewing still succeeds even if status update fails.
            }
        }

        ViewData["Title"] = _localizer["SubmissionDetail"].Value;
        var ctx = submission.Context;
        var model = new SubmissionDetailViewModel
        {
            Id = submission.Id,
            FormId = submission.FormId,
            FormVersionId = submission.FormVersionId,
            FormName = submission.FormName,
            SubmittedAtUtc = submission.SubmittedAtUtc,
            Status = submission.Status,
            StatusLabel = StatusLabel(submission.Status),
            IpAddress = submission.IpAddress,
            UserAgent = submission.UserAgent,
            Page = ctx?.Page,
            Locale = ctx?.Locale,
            Referrer = ctx?.Referrer,
            UtmSource = ctx?.UtmSource,
            UtmMedium = ctx?.UtmMedium,
            UtmCampaign = ctx?.UtmCampaign,
            UtmTerm = ctx?.UtmTerm,
            UtmContent = ctx?.UtmContent,
            Values = submission.Values.Select(v => new SubmissionValueViewModel
            {
                FieldKey = v.FieldKey,
                FieldLabel = v.FieldLabel,
                FieldType = v.FieldType,
                Value = v.Value
            }).ToList(),
            Files = submission.Files.Select(f => new SubmissionFileViewModel
            {
                Id = f.Id,
                FieldKey = f.FieldKey,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                PublicUrl = f.PublicUrl
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> MarkRead(Guid id, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.MarkReadAsync(id, cancellationToken);
        TempData["Success"] = _localizer["SubmissionMarkedRead"].Value;
        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> MarkUnread(Guid id, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.MarkUnreadAsync(id, cancellationToken);
        TempData["Success"] = _localizer["SubmissionMarkedUnread"].Value;
        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> Archive(Guid id, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.ArchiveAsync(id, cancellationToken);
        TempData["Success"] = _localizer["SubmissionArchived"].Value;
        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> Unarchive(Guid id, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.UnarchiveAsync(id, cancellationToken);
        TempData["Success"] = _localizer["SubmissionUnarchived"].Value;
        return RedirectToAction(nameof(Index), new { formId, status = SubmissionStatus.Archived });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> Delete(Guid id, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        try
        {
            await _submissions.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["SubmissionDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> BulkDelete(Guid[] ids, Guid? formId, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.BulkDeleteAsync(ids ?? [], cancellationToken);
        TempData["Success"] = _localizer["SubmissionsDeleted"].Value;
        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageFormSubmissions")]
    public async Task<IActionResult> BulkStatus(
        Guid[] ids,
        SubmissionStatus status,
        Guid? formId,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        await _submissions.BulkSetStatusAsync(ids ?? [], status, cancellationToken);
        TempData["Success"] = _localizer["SubmissionsStatusUpdated"].Value;
        return RedirectToAction(nameof(Index), new { formId });
    }

    [HttpGet]
    [Authorize(Policy = "ExportFormSubmissions")]
    public async Task<IActionResult> Export(
        Guid? formId,
        string? q,
        SubmissionStatus? status,
        string? from,
        string? to,
        Guid[]? ids,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Forms))
            return NotFound();

        var fromUtc = ToUtc(NormalizeDateInput(from), endOfDay: false);
        var toUtc = ToUtc(NormalizeDateInput(to), endOfDay: true);

        var stream = await _submissions.ExportCsvAsync(
            new ExportSubmissionsRequest(ids, formId, status, q, fromUtc, toUtc),
            cancellationToken);

        return File(stream, "text/csv", $"form-submissions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private string StatusLabel(SubmissionStatus status) => status switch
    {
        SubmissionStatus.New => _localizer["SubmissionStatus_New"].Value,
        SubmissionStatus.Read => _localizer["SubmissionStatus_Read"].Value,
        SubmissionStatus.Archived => _localizer["SubmissionStatus_Archived"].Value,
        SubmissionStatus.Processed => _localizer["SubmissionStatus_Processed"].Value,
        SubmissionStatus.Spam => _localizer["SubmissionStatus_Spam"].Value,
        _ => _localizer["SubmissionStatus_New"].Value
    };

    private static string? NormalizeDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeDigits(value.Trim()).Replace('-', '/');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static DateTime? ToUtc(string? localDate, bool endOfDay)
    {
        if (string.IsNullOrWhiteSpace(localDate))
            return null;

        if (!TryParseLocalDate(localDate, endOfDay, out var local))
            return null;

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

    private static bool TryParseLocalDate(string value, bool endOfDay, out DateTime local)
    {
        local = default;

        if (TryParseJalaliDateTime(value, out local))
        {
            if (endOfDay && !value.Contains(':'))
                local = local.Date.AddDays(1).AddTicks(-1);
            return true;
        }

        // Backward-compatible Gregorian yyyy/MM/dd or yyyy-MM-dd.
        if (DateTime.TryParseExact(
                value.Replace('/', '-'),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var gregorian))
        {
            local = endOfDay ? gregorian.Date.AddDays(1).AddTicks(-1) : gregorian.Date;
            return true;
        }

        return false;
    }

    private static bool TryParseJalaliDateTime(string value, out DateTime local)
    {
        local = default;
        var match = JalaliDateRegex.Match(value);
        if (!match.Success)
            return false;

        var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var hour = match.Groups["h"].Success
            ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture)
            : 0;
        var minute = match.Groups["min"].Success
            ? int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture)
            : 0;
        var second = match.Groups["s"].Success
            ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture)
            : 0;

        // Jalali years are typically 1300–1500; avoid mistaking Gregorian as Jalali.
        if (year is < 1200 or > 1600)
            return false;

        try
        {
            local = new PersianCalendar().ToDateTime(year, month, day, hour, minute, second, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string NormalizeDigits(string value)
    {
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var c = buffer[i];
            if (c is >= '\u06F0' and <= '\u06F9')
                buffer[i] = (char)('0' + (c - '\u06F0'));
            else if (c is >= '\u0660' and <= '\u0669')
                buffer[i] = (char)('0' + (c - '\u0660'));
        }

        return new string(buffer);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Tehran",
                TimeSpan.FromHours(3.5),
                "Iran Standard Time",
                "Iran Standard Time");
        }
    }
}
