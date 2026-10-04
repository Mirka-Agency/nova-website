using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Application.Editing;
using CMS.Application.Seo;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.News.Web.Areas.Admin.ViewModels;
using CMS.Modules.News.Web;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.News.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewNews")]
public class NewsArticlesController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    private readonly IArticleService _articles;
    private readonly ICategoryService _categories;
    private readonly IContentAuthorLookup _authors;
    private readonly IFeatureManager _features;
    private readonly IAdminEditLockAccessor _editLocks;
    private readonly ISeoDocumentService _seoDocuments;
    private readonly IStringLocalizer<NewsAdmin> _localizer;
    private readonly ILogger<NewsArticlesController> _logger;

    public NewsArticlesController(
        IArticleService posts,
        ICategoryService categories,
        IContentAuthorLookup authors,
        IFeatureManager features,
        IAdminEditLockAccessor editLocks,
        ISeoDocumentService seoDocuments,
        IStringLocalizer<NewsAdmin> localizer,
        ILogger<NewsArticlesController> logger)
    {
        _articles = posts;
        _categories = categories;
        _authors = authors;
        _features = features;
        _editLocks = editLocks;
        _seoDocuments = seoDocuments;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        ArticleStatus? status = null,
        ArticleKind? kind = null,
        Guid? categoryId = null,
        string? from = null,
        string? to = null,
        string? eventFrom = null,
        string? eventTo = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        ViewData["Title"] = _localizer["Posts"].Value;

        var fromLocal = NormalizeDateInput(from);
        var toLocal = NormalizeDateInput(to);
        var eventFromLocal = NormalizeDateInput(eventFrom);
        var eventToLocal = NormalizeDateInput(eventTo);
        var fromUtc = FilterToUtc(fromLocal, endOfDay: false);
        var toUtc = FilterToUtc(toLocal, endOfDay: true);
        var eventFromUtc = FilterToUtc(eventFromLocal, endOfDay: false);
        var eventToUtc = FilterToUtc(eventToLocal, endOfDay: true);

        var result = await _articles.ListPagedAsync(
            new ArticleListRequest
            {
                Page = page,
                Search = q,
                Status = status,
                Kind = kind,
                CategoryId = categoryId,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                EventFromUtc = eventFromUtc,
                EventToUtc = eventToUtc,
                Sort = sort
            },
            cancellationToken);

        var categories = await _categories.ListAsync(cancellationToken);
        var model = new ArticleIndexViewModel
        {
            Search = q,
            Status = status,
            Kind = kind,
            CategoryId = categoryId,
            FromLocal = fromLocal,
            ToLocal = toLocal,
            EventFromLocal = eventFromLocal,
            EventToLocal = eventToLocal,
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
            Items = result.Items.Select(p => new ArticleListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Status = p.Status == ArticleStatus.Published ? _localizer["Published"] : _localizer["Draft"],
                Kind = p.Kind == ArticleKind.Event ? _localizer["EventKind"] : _localizer["NewsKind"],
                CategoryName = p.CategoryName,
                CreatedAtUtc = p.CreatedAtUtc,
                CreatedAtLocal = ToIranDate(p.CreatedAtUtc),
                EventStartLocal = p.EventStartAtUtc.HasValue ? ToIranDateTime(p.EventStartAtUtc.Value) : null
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "ManageNews")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var existingDraftId = await _articles.FindUserDraftIdAsync(userId, cancellationToken);
        if (existingDraftId.HasValue)
            return RedirectToAction(nameof(Edit), new { id = existingDraftId.Value, resumeDraft = true });

        var draftId = await _articles.GetOrCreateUserDraftAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = draftId });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ArticleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        var existingDraftId = await _articles.FindUserDraftIdAsync(userId, cancellationToken);
        if (existingDraftId.HasValue)
            return RedirectToAction(nameof(Edit), new { id = existingDraftId.Value, resumeDraft = true });

        var draftId = await _articles.GetOrCreateUserDraftAsync(userId, cancellationToken);
        return RedirectToAction(nameof(Edit), new { id = draftId });
    }

    [HttpPost]
    [Authorize(Policy = "ManageNews")]
    public async Task<IActionResult> DiscardDraft(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Challenge();

        try
        {
            var newDraftId = await _articles.DiscardUserDraftAsync(userId, id, cancellationToken);
            _logger.LogInformation("Admin action: discarded news draft {PostId} by {UserId}", id, userId);
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
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        var post = await _articles.GetAsync(id, cancellationToken);
        if (post is null)
            return NotFound();

        var lockResult = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.NewsArticle, id, cancellationToken);
        if (lockResult is null)
            return Challenge();
        EditLockViewBag.Apply(ViewBag, lockResult, EditLockEntityTypes.NewsArticle, id);

        ViewBag.ResumeDraft = resumeDraft && post.Status == ArticleStatus.Draft;
        ViewData["Title"] = _localizer["EditPost"].Value;
        var model = new ArticleFormViewModel
        {
            Id = post.Id,
            Title = string.Equals(post.Title, ArticleDraftDefaults.Title, StringComparison.Ordinal)
                ? string.Empty
                : post.Title,
            Slug = post.Slug,
            Body = post.Body,
            Excerpt = post.Excerpt,
            Kind = post.Kind,
            CoverImageUrl = post.CoverImageUrl,
            GalleryImages = ArticleGalleryJson.Parse(post.GalleryJson)
                .Select(x => new ArticleGalleryImageFormItem { Url = x.Url, AltText = x.AltText })
                .ToList(),
            AttachmentUrl = post.AttachmentUrl,
            AttachmentFileName = post.AttachmentFileName,
            CategoryId = post.CategoryId,
            AuthorUserId = post.AuthorUserId,
            Publish = post.Status == ArticleStatus.Published,
            PublishedAtLocal = ToIranLocal(post.PublishedAtUtc),
            EventStartAtLocal = ToIranLocal(post.EventStartAtUtc),
            EventEndAtLocal = ToIranLocal(post.EventEndAtUtc),
            EventStartAtUtc = post.EventStartAtUtc,
            EventEndAtUtc = post.EventEndAtUtc,
            Location = post.Location,
            EventInfoItems = ArticleEventInfoJson.Parse(post.EventInfoJson)
                .Select(x => new ArticleEventInfoFormItem { Label = x.Label, Value = x.Value })
                .ToList(),
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            SeoKeywords = post.SeoKeywords,
            CanonicalUrl = post.CanonicalUrl,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl
        };

        if (await _features.IsEnabledAsync(FeatureNames.Seo))
        {
            var seoDoc = await _seoDocuments.GetAsync(SeoContentTypeKeys.NewsArticle, post.Id, cancellationToken);
            model.Seo = SeoEditorHelper.CreateFields(
                SeoContentTypeKeys.NewsArticle,
                post.Id,
                seoDoc,
                previewTitle: FirstNonEmpty(post.MetaTitle, post.Title),
                previewDescription: FirstNonEmpty(post.MetaDescription, post.Excerpt),
                previewUrl: $"/news/{post.Slug}");
        }

        return View(await BuildFormAsync(model, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, ArticleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.News))
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
            await _articles.UpdateAsync(id, await ToCommandAsync(model, cancellationToken), cancellationToken);
            await UpsertSeoAsync(id, model, cancellationToken);
            _logger.LogInformation("Admin action: updated news article {PostId}", id);
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
        if (!await _features.IsEnabledAsync(FeatureNames.News))
            return NotFound();

        await _articles.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted news article {PostId}", id);
        TempData["Success"] = _localizer["PostDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            var modelKey = key switch
            {
                nameof(ArticleFormViewModel.EventStartAtLocal) or "EventStartAtUtc" => nameof(ArticleFormViewModel.EventStartAtLocal),
                nameof(ArticleFormViewModel.EventEndAtLocal) or "EventEndAtUtc" => nameof(ArticleFormViewModel.EventEndAtLocal),
                "PublishedAtUtc" => nameof(ArticleFormViewModel.PublishedAtLocal),
                _ => key
            };

            foreach (var message in messages)
                ModelState.AddModelError(modelKey, message);
        }
    }

    private async Task<SaveArticleCommand> ToCommandAsync(ArticleFormViewModel model, CancellationToken cancellationToken)
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

        var galleryJson = ArticleGalleryJson.Serialize(
            (model.GalleryImages ?? []).Select(x => new ArticleGalleryImageDto(x.Url, x.AltText)));
        var eventInfoJson = ArticleEventInfoJson.Serialize(
            (model.EventInfoItems ?? []).Select(x => new ArticleEventInfoItemDto(x.Label, x.Value)));

        return new SaveArticleCommand(
            string.IsNullOrWhiteSpace(model.Title) ? ArticleDraftDefaults.Title : model.Title.Trim(),
            model.Slug,
            model.Body ?? string.Empty,
            NullIfWhiteSpace(model.Excerpt),
            model.Kind,
            model.CategoryId,
            NullIfWhiteSpace(model.CoverImageUrl),
            galleryJson,
            NullIfWhiteSpace(model.AttachmentUrl),
            NullIfWhiteSpace(model.AttachmentFileName),
            model.Publish,
            ToUtc(model.PublishedAtLocal, nameof(ArticleFormViewModel.PublishedAtLocal)),
            ToUtc(model.EventStartAtLocal, nameof(ArticleFormViewModel.EventStartAtLocal)),
            ToUtc(model.EventEndAtLocal, nameof(ArticleFormViewModel.EventEndAtLocal)),
            NullIfWhiteSpace(model.Location),
            eventInfoJson,
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

    private async Task<ArticleFormViewModel> BuildFormAsync(ArticleFormViewModel model, CancellationToken cancellationToken)
    {
        var categories = await _categories.ListAsync(cancellationToken);
        var authors = await _authors.ListContentAuthorsAsync(cancellationToken);
        model.GalleryImages ??= [];
        model.EventInfoItems ??= [];

        model.Kinds =
        [
            new SelectListItem(_localizer["NewsKind"], nameof(ArticleKind.News), model.Kind == ArticleKind.News),
            new SelectListItem(_localizer["EventKind"], nameof(ArticleKind.Event), model.Kind == ArticleKind.Event)
        ];

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

        model.Seo ??= SeoEditorHelper.CreateFields(SeoContentTypeKeys.NewsArticle, model.Id);
        model.Seo.ContentType = SeoContentTypeKeys.NewsArticle;
        model.Seo.ContentId = model.Id;
        SeoEditorHelper.EnsureSchemaOptions(model.Seo);

        return model;
    }

    private async Task UpsertSeoAsync(Guid contentId, ArticleFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return;

        model.Seo ??= SeoEditorHelper.CreateFields(SeoContentTypeKeys.NewsArticle, contentId);
        model.Seo.ContentType = SeoContentTypeKeys.NewsArticle;
        await _seoDocuments.UpsertAsync(SeoEditorHelper.ToSaveCommand(model.Seo, contentId), cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ToIranDate(DateTime utc)
    {
        var value = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
        var persian = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(local):0000}/{persian.GetMonth(local):00}/{persian.GetDayOfMonth(local):00}");
    }

    private static string ToIranDateTime(DateTime utc) => ToIranLocal(utc)!;

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

    private static DateTime? ToUtc(string? jalaliLocal, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(jalaliLocal))
            return null;

        if (!TryParseJalaliDateTime(jalaliLocal, out var local))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [fieldName] = ["تاریخ نامعتبر است."]
            });

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
        if (await _editLocks.CurrentUserHoldsAsync(EditLockEntityTypes.NewsArticle, id, cancellationToken: cancellationToken))
            return true;

        var acquired = await _editLocks.TryAcquireForCurrentUserAsync(
            EditLockEntityTypes.NewsArticle, id, cancellationToken);
        return acquired?.Acquired == true;
    }
}
