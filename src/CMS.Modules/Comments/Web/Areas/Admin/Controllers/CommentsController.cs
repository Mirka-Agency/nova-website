using System.Globalization;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Comments.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewComments")]
public class CommentsController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    private readonly ICommentService _comments;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<CommentsAdmin> _localizer;

    public CommentsController(
        ICommentService comments,
        IFeatureManager features,
        IStringLocalizer<CommentsAdmin> localizer)
    {
        _comments = comments;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        CommentTargetType? targetType = null,
        CommentStatus? status = null,
        string? q = null,
        string? search = null,
        string? from = null,
        string? to = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        var query = !string.IsNullOrWhiteSpace(q) ? q : search;
        var fromLocal = NormalizeDateInput(from);
        var toLocal = NormalizeDateInput(to);
        var fromUtc = FilterToUtc(fromLocal, endOfDay: false);
        var toUtc = FilterToUtc(toLocal, endOfDay: true);

        var filter = new CommentListFilterViewModel
        {
            TargetType = targetType,
            Status = status,
            Search = query,
            FromLocal = fromLocal,
            ToLocal = toLocal,
            Sort = sort
        };

        ViewData["Title"] = TargetTitle(targetType);
        ViewBag.StatusOptions = BuildStatusOptions(status);
        ViewBag.TargetOptions = BuildTargetOptions(targetType);

        var result = await _comments.ListPagedAsync(
            new CommentListQuery(targetType, status, query, fromUtc, toUtc, sort, page),
            cancellationToken);

        var model = new CommentIndexViewModel
        {
            Filter = filter,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            Items = result.Items.Select(item => new CommentListItemViewModel
            {
                Id = item.Id,
                TargetType = item.TargetType,
                TargetTitle = item.TargetTitle,
                AuthorName = item.AuthorName,
                AuthorEmail = item.AuthorEmail,
                AuthorPhone = item.AuthorPhone,
                Body = item.Body,
                Status = item.Status,
                PublishedAtLocal = ToIranLocal(item.PublishedAtUtc)!
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        var comment = await _comments.GetAsync(id, cancellationToken);
        if (comment is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditComment"].Value;
        return View(new CommentEditViewModel
        {
            Id = comment.Id,
            TargetType = comment.TargetType,
            TargetTitle = comment.TargetTitle,
            AuthorName = comment.AuthorName,
            AuthorEmail = comment.AuthorEmail,
            AuthorPhone = comment.AuthorPhone,
            Status = comment.Status,
            Body = comment.Body,
            PublishedAtLocal = ToIranLocal(comment.PublishedAtUtc)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CommentEditViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        ViewData["Title"] = _localizer["EditComment"].Value;
        model.Id = id;

        try
        {
            var publishedAt = ToUtc(model.PublishedAtLocal);
            if (!publishedAt.HasValue)
            {
                ModelState.AddModelError(nameof(model.PublishedAtLocal), _localizer["InvalidPublishDate"].Value);
                return View(model);
            }

            await _comments.UpdateAsync(id, new UpdateCommentCommand(model.Body, publishedAt.Value), cancellationToken);
            TempData["Success"] = _localizer["CommentUpdated"].Value;
            return RedirectToAction(nameof(Index), new CommentListFilterViewModel
            {
                TargetType = model.TargetType
            }.ToRouteValues());
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
            {
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            }
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Approve(
        Guid id,
        CommentTargetType? targetType,
        CommentStatus? status,
        string? q,
        string? from,
        string? to,
        string? sort,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        try
        {
            await _comments.ApproveAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CommentApproved"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), BuildFilterRouteValues(targetType, status, q, from, to, sort));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(
        Guid id,
        CommentTargetType? targetType,
        CommentStatus? status,
        string? q,
        string? from,
        string? to,
        string? sort,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        try
        {
            await _comments.RejectAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CommentRejected"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), BuildFilterRouteValues(targetType, status, q, from, to, sort));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(
        Guid id,
        CommentTargetType? targetType,
        CommentStatus? status,
        string? q,
        string? from,
        string? to,
        string? sort,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Comments))
            return NotFound();

        try
        {
            await _comments.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CommentDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), BuildFilterRouteValues(targetType, status, q, from, to, sort));
    }

    private static object BuildFilterRouteValues(
        CommentTargetType? targetType,
        CommentStatus? status,
        string? q,
        string? from,
        string? to,
        string? sort) =>
        new CommentListFilterViewModel
        {
            TargetType = targetType,
            Status = status,
            Search = q,
            FromLocal = from,
            ToLocal = to,
            Sort = sort
        }.ToRouteValues();

    private string TargetTitle(CommentTargetType? targetType) =>
        targetType switch
        {
            CommentTargetType.BlogPost => _localizer["BlogComments"].Value,
            CommentTargetType.Event => _localizer["EventComments"].Value,
            CommentTargetType.Product => _localizer["ProductComments"].Value,
            CommentTargetType.ProductCategory => _localizer["ProductCategoryComments"].Value,
            _ => _localizer["AllComments"].Value
        };

    private IEnumerable<SelectListItem> BuildStatusOptions(CommentStatus? selected) =>
    [
        new SelectListItem(_localizer["AllStatuses"].Value, "", !selected.HasValue),
        new SelectListItem(_localizer["StatusPending"].Value, ((int)CommentStatus.Pending).ToString(), selected == CommentStatus.Pending),
        new SelectListItem(_localizer["StatusApproved"].Value, ((int)CommentStatus.Approved).ToString(), selected == CommentStatus.Approved),
        new SelectListItem(_localizer["StatusRejected"].Value, ((int)CommentStatus.Rejected).ToString(), selected == CommentStatus.Rejected)
    ];

    private IEnumerable<SelectListItem> BuildTargetOptions(CommentTargetType? selected) =>
    [
        new SelectListItem(_localizer["AllComments"].Value, "", !selected.HasValue),
        new SelectListItem(_localizer["BlogComments"].Value, ((int)CommentTargetType.BlogPost).ToString(), selected == CommentTargetType.BlogPost),
        new SelectListItem(_localizer["EventComments"].Value, ((int)CommentTargetType.Event).ToString(), selected == CommentTargetType.Event),
        new SelectListItem(_localizer["ProductComments"].Value, ((int)CommentTargetType.Product).ToString(), selected == CommentTargetType.Product),
        new SelectListItem(_localizer["ProductCategoryComments"].Value, ((int)CommentTargetType.ProductCategory).ToString(), selected == CommentTargetType.ProductCategory)
    ];

    private static DateTime? FilterToUtc(string? localDate, bool endOfDay)
    {
        if (string.IsNullOrWhiteSpace(localDate))
            return null;

        if (!TryParseJalaliDateTime(localDate, out var local))
            return null;

        if (endOfDay && !localDate.Contains(':'))
            local = local.Date.AddDays(1).AddTicks(-1);

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

    private static string? NormalizeDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeDigits(value.Trim()).Replace('-', '/');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? ToIranLocal(DateTime utc)
    {
        var value = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
        var persian = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(local):0000}/{persian.GetMonth(local):00}/{persian.GetDayOfMonth(local):00} {local.Hour:00}:{local.Minute:00}");
    }

    private static DateTime? ToUtc(string? jalaliLocal)
    {
        if (string.IsNullOrWhiteSpace(jalaliLocal))
            return null;

        if (!TryParseJalaliDateTime(jalaliLocal, out var local))
            return null;

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

    private static bool TryParseJalaliDateTime(string value, out DateTime local)
    {
        local = default;
        var normalized = NormalizeDigits(value.Trim());
        var match = Regex.Match(
            normalized,
            @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$");
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

        try
        {
            local = new PersianCalendar().ToDateTime(year, month, day, hour, minute, second, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeDigits(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '\u06F0' and <= '\u06F9')
                chars[i] = (char)('0' + (chars[i] - '\u06F0'));
            else if (chars[i] is >= '\u0660' and <= '\u0669')
                chars[i] = (char)('0' + (chars[i] - '\u0660'));
        }
        return new string(chars);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Iran Standard Time",
            TimeSpan.FromHours(3.5),
            "Iran Standard Time",
            "Iran Standard Time");
    }
}
