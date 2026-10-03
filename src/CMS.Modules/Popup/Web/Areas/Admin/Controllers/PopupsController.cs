using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Popup.Application.Popups;
using CMS.Modules.Popup.Domain.Enums;
using CMS.Modules.Popup.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Popup.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewPopup")]
public class PopupsController : Controller
{
    private readonly IPopupService _popups;
    private readonly IFormService _forms;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<PopupAdmin> _localizer;
    private readonly ILogger<PopupsController> _logger;

    public PopupsController(
        IPopupService popups,
        IFormService forms,
        IFeatureManager features,
        IStringLocalizer<PopupAdmin> localizer,
        ILogger<PopupsController> logger)
    {
        _popups = popups;
        _forms = forms;
        _features = features;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        bool? active = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        ViewData["Title"] = _localizer["Popups"].Value;
        ViewBag.Search = q;
        ViewBag.HasActiveFilters = active.HasValue;

        var result = await _popups.ListPagedAsync(
            new PopupListRequest { Page = page, Search = q, IsActive = active },
            cancellationToken);

        var model = new PopupIndexViewModel
        {
            Search = q,
            IsActive = active,
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(p => new PopupListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                IsActive = p.IsActive,
                TriggerType = p.TriggerType,
                TriggerLabel = TriggerLabel(p.TriggerType),
                FrequencyLabel = FrequencyLabel(p.Frequency),
                PageTargetLabel = PageTargetLabel(p.PageTargetMode),
                HasForm = p.FormId.HasValue
            }).ToList()
        };

        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.TotalCount = result.TotalCount;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        ViewData["Title"] = _localizer["CreatePopup"].Value;
        return View(await PrepareFormAsync(new PopupFormViewModel(), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PopupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        ViewData["Title"] = _localizer["CreatePopup"].Value;
        model = await PrepareFormAsync(model, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _popups.CreateAsync(ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: created popup {PopupId}", id);
            TempData["Success"] = _localizer["PopupCreated"].Value;
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

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        var item = await _popups.GetAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditPopup"].Value;
        var model = new PopupFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            BodyText = item.BodyText,
            ImageUrl = item.ImageUrl,
            ContentHtml = item.ContentHtml,
            IsActive = item.IsActive,
            FormId = item.FormId,
            CtaText = item.CtaText,
            CtaAction = item.CtaAction,
            CtaUrl = item.CtaUrl,
            CtaTargetPopupId = item.CtaTargetPopupId,
            TriggerType = item.TriggerType,
            TriggerDelaySeconds = item.TriggerDelaySeconds ?? 5,
            TriggerScrollPercent = item.TriggerScrollPercent ?? 50,
            TriggerSelector = item.TriggerSelector,
            ShowOverlay = item.ShowOverlay,
            ShowCloseButton = item.ShowCloseButton,
            CloseOnOverlayClick = item.CloseOnOverlayClick,
            CloseOnEscape = item.CloseOnEscape,
            LockBodyScroll = item.LockBodyScroll,
            EnableContentScroll = item.EnableContentScroll,
            Frequency = item.Frequency,
            PageTargetMode = item.PageTargetMode,
            PagePaths = item.PagePaths,
            SortOrder = item.SortOrder
        };
        return View(await PrepareFormAsync(model, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, PopupFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        ViewData["Title"] = _localizer["EditPopup"].Value;
        model.Id = id;
        model = await PrepareFormAsync(model, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _popups.UpdateAsync(id, ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: updated popup {PopupId}", id);
            TempData["Success"] = _localizer["PopupUpdated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        await _popups.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted popup {PopupId}", id);
        TempData["Success"] = _localizer["PopupDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return NotFound();

        var isActive = await _popups.ToggleActiveAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: set popup {PopupId} active={IsActive}", id, isActive);
        TempData["Success"] = isActive ? _localizer["PopupActivated"].Value : _localizer["PopupDeactivated"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static SavePopupCommand ToCommand(PopupFormViewModel model) =>
        new(
            model.Title,
            model.Slug,
            model.BodyText,
            NullIfWhiteSpace(model.ImageUrl),
            model.ContentHtml,
            model.IsActive,
            model.FormId,
            model.CtaText,
            model.CtaAction,
            NullIfWhiteSpace(model.CtaUrl),
            model.CtaTargetPopupId,
            model.TriggerType,
            model.TriggerDelaySeconds,
            model.TriggerScrollPercent,
            NullIfWhiteSpace(model.TriggerSelector),
            null,
            model.ShowOverlay,
            model.ShowCloseButton,
            model.CloseOnOverlayClick,
            model.CloseOnEscape,
            model.LockBodyScroll,
            model.EnableContentScroll,
            model.Frequency,
            model.PageTargetMode,
            model.PagePaths,
            null,
            model.SortOrder);

    private async Task<PopupFormViewModel> PrepareFormAsync(PopupFormViewModel model, CancellationToken cancellationToken)
    {
        model.CtaActionOptions =
        [
            new SelectListItem(_localizer["CtaActionNone"].Value, PopupCtaActions.None, model.CtaAction == PopupCtaActions.None),
            new SelectListItem(_localizer["CtaActionUrl"].Value, PopupCtaActions.Url, model.CtaAction == PopupCtaActions.Url),
            new SelectListItem(_localizer["CtaActionClose"].Value, PopupCtaActions.Close, model.CtaAction == PopupCtaActions.Close),
            new SelectListItem(_localizer["CtaActionOpenPopup"].Value, PopupCtaActions.OpenPopup, model.CtaAction == PopupCtaActions.OpenPopup)
        ];

        model.FrequencyOptions =
        [
            new SelectListItem(_localizer["FrequencyAlways"].Value, nameof(PopupFrequency.Always), model.Frequency == PopupFrequency.Always),
            new SelectListItem(_localizer["FrequencyEveryVisit"].Value, nameof(PopupFrequency.EveryVisit), model.Frequency == PopupFrequency.EveryVisit),
            new SelectListItem(_localizer["FrequencyOncePerSession"].Value, nameof(PopupFrequency.OncePerSession), model.Frequency == PopupFrequency.OncePerSession),
            new SelectListItem(_localizer["FrequencyOncePerBrowser"].Value, nameof(PopupFrequency.OncePerBrowser), model.Frequency == PopupFrequency.OncePerBrowser)
        ];

        model.PageTargetOptions =
        [
            new SelectListItem(_localizer["PageTargetAll"].Value, nameof(PopupPageTargetMode.All), model.PageTargetMode == PopupPageTargetMode.All),
            new SelectListItem(_localizer["PageTargetHomepage"].Value, nameof(PopupPageTargetMode.Homepage), model.PageTargetMode == PopupPageTargetMode.Homepage),
            new SelectListItem(_localizer["PageTargetInclude"].Value, nameof(PopupPageTargetMode.Include), model.PageTargetMode == PopupPageTargetMode.Include),
            new SelectListItem(_localizer["PageTargetExclude"].Value, nameof(PopupPageTargetMode.Exclude), model.PageTargetMode == PopupPageTargetMode.Exclude)
        ];

        var formOptions = new List<SelectListItem>
        {
            new(_localizer["NoForm"].Value, string.Empty, !model.FormId.HasValue)
        };

        if (await _features.IsEnabledAsync(FeatureNames.Forms))
        {
            var forms = await _forms.ListAsync(cancellationToken);
            foreach (var form in forms.Where(f => f.Status == FormStatus.Published || f.IsPublished)
                         .Where(f => f.FieldCount > 0)
                         .OrderBy(f => f.Name))
            {
                formOptions.Add(new SelectListItem(
                    form.Name,
                    form.Id.ToString(),
                    model.FormId == form.Id));
            }

            if (model.FormId is Guid selectedId
                && formOptions.All(o => o.Value != selectedId.ToString()))
            {
                var selected = forms.FirstOrDefault(f => f.Id == selectedId);
                if (selected is not null)
                {
                    formOptions.Insert(1, new SelectListItem(
                        $"{selected.Name} ({_localizer["LinkedFormEmpty"].Value})",
                        selected.Id.ToString(),
                        selected: true));
                }
            }
        }

        model.FormOptions = formOptions;

        var popupOptions = new List<SelectListItem>
        {
            new(_localizer["SelectTargetPopup"].Value, string.Empty, !model.CtaTargetPopupId.HasValue)
        };
        foreach (var option in await _popups.ListOptionsAsync(model.Id, cancellationToken))
        {
            popupOptions.Add(new SelectListItem(
                option.Title,
                option.Id.ToString(),
                model.CtaTargetPopupId == option.Id));
        }

        model.PopupTargetOptions = popupOptions;
        model.DelaySecondsOptions = BuildIntOptions(
            [0, 1, 2, 3, 5, 7, 10, 15, 20, 30, 45, 60],
            model.TriggerDelaySeconds ?? 5,
            v => v == 0
                ? _localizer["TriggerSecondsImmediate"].Value
                : string.Format(_localizer["TriggerSecondsOption"].Value, v));
        model.ScrollPercentOptions = BuildIntOptions(
            [10, 25, 40, 50, 60, 75, 90, 100],
            model.TriggerScrollPercent ?? 50,
            v => string.Format(_localizer["TriggerScrollOption"].Value, v));
        return model;
    }

    private static IReadOnlyList<SelectListItem> BuildIntOptions(
        int[] presets,
        int current,
        Func<int, string> label)
    {
        var values = presets.ToList();
        if (!values.Contains(current))
            values.Add(current);
        values.Sort();
        return values
            .Select(v => new SelectListItem(label(v), v.ToString(), v == current))
            .ToList();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string TriggerLabel(string triggerType) =>
        triggerType.ToLowerInvariant() switch
        {
            PopupTriggerTypes.Timer => _localizer["TriggerTimer"].Value,
            PopupTriggerTypes.Scroll => _localizer["TriggerScroll"].Value,
            _ => _localizer["TriggerManual"].Value
        };

    private string FrequencyLabel(PopupFrequency frequency) =>
        frequency switch
        {
            PopupFrequency.EveryVisit => _localizer["FrequencyEveryVisit"].Value,
            PopupFrequency.OncePerSession => _localizer["FrequencyOncePerSession"].Value,
            PopupFrequency.OncePerBrowser => _localizer["FrequencyOncePerBrowser"].Value,
            _ => _localizer["FrequencyAlways"].Value
        };

    private string PageTargetLabel(PopupPageTargetMode mode) =>
        mode switch
        {
            PopupPageTargetMode.Homepage => _localizer["PageTargetHomepage"].Value,
            PopupPageTargetMode.Include => _localizer["PageTargetInclude"].Value,
            PopupPageTargetMode.Exclude => _localizer["PageTargetExclude"].Value,
            _ => _localizer["PageTargetAll"].Value
        };

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }
}
