using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Application.TeamItems;
using CMS.Modules.Team.Domain.Enums;
using CMS.Modules.Team.Web.Areas.Admin.ViewModels;
using CMS.Modules.Team.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Team.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewTeam")]
public class TeamItemsController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    private readonly ITeamItemService _posts;
    private readonly ICategoryService _categories;
    private readonly IContentAuthorLookup _authors;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly IStringLocalizer<TeamAdmin> _localizer;
    private readonly ILogger<TeamItemsController> _logger;

    public TeamItemsController(
        ITeamItemService posts,
        ICategoryService categories,
        IContentAuthorLookup authors,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        IStringLocalizer<TeamAdmin> localizer,
        ILogger<TeamItemsController> logger)
    {
        _posts = posts;
        _categories = categories;
        _authors = authors;
        _features = features;
        _editLocks = editLocks;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        TeamStatus? status = null,
        Guid? categoryId = null,
        string? from = null,
        string? to = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["TeamItems"].Value;

        var fromLocal = NormalizeDateInput(from);
        var toLocal = NormalizeDateInput(to);
        var fromUtc = FilterToUtc(fromLocal, endOfDay: false);
        var toUtc = FilterToUtc(toLocal, endOfDay: true);

        var result = await _posts.ListPagedAsync(
            new TeamItemListRequest
            {
                Page = page,
                Search = q,
                Status = status,
                CategoryId = categoryId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Sort = sort
            },
            cancellationToken);

        var categories = await _categories.ListAsync(cancellationToken);
        var model = new TeamItemIndexViewModel
        {
            Search = q,
            Status = status,
            CategoryId = categoryId,
            FromLocal = fromLocal,
            ToLocal = toLocal,
            Sort = sort,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            Categories =
            [
                new SelectListItem(_localizer["AllCategories"], string.Empty, categoryId is null),
                .. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), categoryId == c.Id))
            ],
            Items = result.Items.Select(p => new TeamItemListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Status = p.Status == TeamStatus.Published ? _localizer["Published"] : _localizer["Draft"],
                IsPublished = p.Status == TeamStatus.Published,
                CategoryName = p.CategoryName,
                CreatedAtUtc = p.CreatedAtUtc,
                CreatedAtLocal = ToIranDate(p.CreatedAtUtc)
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "ManageTeam")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var existingDraftId = await _posts.FindUserDraftIdAsync(userId, cancellationToken);
        if (existingDraftId.HasValue)
            return RedirectToAction(nameof(Edit), new { id = existingDraftId.Value, resumeDraft = true });

        var draftId = await _posts.GetOrCreateUserDraftAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = draftId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(TeamItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var existingDraftId = await _posts.FindUserDraftIdAsync(userId, cancellationToken);
        if (existingDraftId.HasValue)
            return RedirectToAction(nameof(Edit), new { id = existingDraftId.Value, resumeDraft = true });

        var draftId = await _posts.GetOrCreateUserDraftAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = draftId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageTeam")]
    public async Task<IActionResult> DiscardDraft(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var newDraftId = await _posts.DiscardUserDraftAsync(userId, id, cancellationToken);
            _logger.LogInformation("Admin action: discarded service draft {PostId} by {UserId}", id, userId);
            return RedirectToAction(nameof(Edit), new { id = newDraftId });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, bool resumeDraft = false, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var post = await _posts.GetAsync(id, cancellationToken);
        if (post is null)
            return NotFound();

        var lockResult = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.TeamItem, id, cancellationToken);
        if (lockResult is null)
            return Challenge();
        EditLockViewBag.Apply(ViewBag, lockResult, EditLockEntityTypes.TeamItem, id);

        ViewBag.ResumeDraft = resumeDraft && post.Status == TeamStatus.Draft;
        ViewData["Title"] = _localizer["EditPost"].Value;
        var model = new TeamItemFormViewModel
        {
            Id = post.Id,
            Title = string.Equals(post.Title, TeamItemDraftDefaults.Title, StringComparison.Ordinal)
                ? string.Empty
                : post.Title,
            Subtitle = post.Subtitle,
            Slug = post.Slug,
            Body = post.Body,
            Excerpt = post.Excerpt,
            Highlights = post.Highlights,
            SpecialtyPathItems = TeamSpecialtyPathJson.Parse(post.SpecialtyPathJson)
                .Select(x => new TeamSpecialtyPathItemViewModel { Title = x.Title, Text = x.Text })
                .ToList(),
            EducationPathItems = TeamEducationPathJson.Parse(post.EducationPathJson)
                .Select(x => new TeamEducationPathItemViewModel { Year = x.Year, Title = x.Title, Place = x.Place })
                .ToList(),
            CoverImageUrl = post.CoverImageUrl,
            AvatarImageUrl = post.AvatarImageUrl,
            CategoryId = post.CategoryId,
            AuthorUserId = post.AuthorUserId,
            // Fresh auto-drafts default Publish on (user preference); existing drafts keep unchecked.
            Publish = post.Status == TeamStatus.Published
                || string.Equals(post.Title, TeamItemDraftDefaults.Title, StringComparison.Ordinal),
            PublishedAtLocal = ToIranLocal(post.PublishedAtUtc),
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            SeoKeywords = post.SeoKeywords,
            CanonicalUrl = post.CanonicalUrl,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl
        };
        return View(await BuildFormAsync(model, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, TeamItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        model.Id = id;
        model = await BuildFormAsync(model, cancellationToken);
        ViewData["Title"] = _localizer["EditPost"].Value;

        if (model.Publish && string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), _localizer["TitleRequired"].Value);

        if (!ModelState.IsValid)
            return View(model);

        if (!await EnsureEditLockAsync(id, cancellationToken))
        {
            TempData["Error"] = _localizer["EditLockLost"].Value;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _posts.UpdateAsync(id, await ToCommandAsync(model, cancellationToken), cancellationToken);
            _logger.LogInformation("Admin action: updated service post {PostId}", id);
            TempData["Success"] = _localizer["PostUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        await _posts.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted service post {PostId}", id);
        TempData["Success"] = _localizer["PostDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }

    private async Task<SaveTeamItemCommand> ToCommandAsync(TeamItemFormViewModel model, CancellationToken cancellationToken)
    {
        string? authorDisplayName = null;
        if (!string.IsNullOrWhiteSpace(model.AuthorUserId))
        {
            authorDisplayName = await _authors.GetDisplayNameAsync(model.AuthorUserId, cancellationToken);
            if (authorDisplayName is null)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [nameof(model.AuthorUserId)] = [_localizer["AuthorNotFound"].Value]
                });
            }
        }

        var specialtyPathJson = TeamSpecialtyPathJson.Serialize(
            (model.SpecialtyPathItems ?? [])
                .Select(x => new TeamSpecialtyPathItemDto(x.Title, x.Text)));
        var educationPathJson = TeamEducationPathJson.Serialize(
            (model.EducationPathItems ?? [])
                .Select(x => new TeamEducationPathItemDto(x.Year, x.Title, x.Place)));

        return new SaveTeamItemCommand(
            string.IsNullOrWhiteSpace(model.Title) ? TeamItemDraftDefaults.Title : model.Title.Trim(),
            NullIfWhiteSpace(model.Subtitle),
            model.Slug,
            model.Body ?? string.Empty,
            NullIfWhiteSpace(model.Excerpt),
            NullIfWhiteSpace(model.Highlights),
            specialtyPathJson,
            educationPathJson,
            model.CategoryId,
            NullIfWhiteSpace(model.CoverImageUrl),
            NullIfWhiteSpace(model.AvatarImageUrl),
            model.Publish,
            ToUtc(model.PublishedAtLocal),
            NullIfWhiteSpace(model.AuthorUserId),
            authorDisplayName,
            NullIfWhiteSpace(model.MetaTitle),
            NullIfWhiteSpace(model.MetaDescription),
            NullIfWhiteSpace(model.SeoKeywords),
            NullIfWhiteSpace(model.CanonicalUrl),
            NullIfWhiteSpace(model.OgTitle),
            NullIfWhiteSpace(model.OgDescription),
            NullIfWhiteSpace(model.OgImageUrl));
    }

    private async Task<TeamItemFormViewModel> BuildFormAsync(TeamItemFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categories.ListAsync(cancellationToken);
        var authors = await _authors.ListContentAuthorsAsync(cancellationToken);

        model.Categories =
        [
            new SelectListItem(_localizer["NoCategory"], string.Empty, model.CategoryId is null),
            .. categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), model.CategoryId == c.Id))
        ];

        model.Authors =
        [
            new SelectListItem(_localizer["NoAuthor"], string.Empty, string.IsNullOrWhiteSpace(model.AuthorUserId)),
            .. authors.Select(a => new SelectListItem(
                a.DisplayName,
                a.Id,
                string.Equals(model.AuthorUserId, a.Id, StringComparison.Ordinal)))
        ];

        model.SpecialtyPathItems ??= [];
        model.EducationPathItems ??= [];
        return model;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ToIranLocal(DateTime? utc)
    {
        if (!utc.HasValue)
            return null;

        var value = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
        var persian = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(local):0000}/{persian.GetMonth(local):00}/{persian.GetDayOfMonth(local):00} {local.Hour:00}:{local.Minute:00}");
    }

    private static string ToIranDate(DateTime utc)
    {
        var value = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
        var persian = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(local):0000}/{persian.GetMonth(local):00}/{persian.GetDayOfMonth(local):00}");
    }

    private static DateTime? ToUtc(string? jalaliLocal)
    {
        if (string.IsNullOrWhiteSpace(jalaliLocal))
            return null;

        if (!TryParseJalaliDateTime(jalaliLocal, out var local))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(TeamItemFormViewModel.PublishedAtLocal)] = ["تاریخ انتشار نامعتبر است."]
            });

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

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

    private async Task<bool> EnsureEditLockAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.TeamItem, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.TeamItem, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
