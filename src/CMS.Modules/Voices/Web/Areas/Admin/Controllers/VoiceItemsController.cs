using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Modules.Voices.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Voices.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewVoices")]
public class VoiceItemsController : Controller
{
    private readonly IVoiceItemService _voices;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<VoicesAdmin> _localizer;
    private readonly ILogger<VoiceItemsController> _logger;

    public VoiceItemsController(
        IVoiceItemService voices,
        IFeatureManager features,
        IStringLocalizer<VoicesAdmin> localizer,
        ILogger<VoiceItemsController> logger)
    {
        _voices = voices;
        _features = features;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        bool? published = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        ViewData["Title"] = _localizer["VoiceItems"].Value;
        ViewBag.Search = q;
        ViewBag.HasActiveFilters = published.HasValue;

        var result = await _voices.ListPagedAsync(
            new VoiceItemListRequest { Page = page, Search = q, IsPublished = published },
            cancellationToken);

        var model = new VoiceItemIndexViewModel
        {
            Search = q,
            IsPublished = published,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(v => new VoiceItemListItemViewModel
            {
                Id = v.Id,
                CustomerName = v.CustomerName,
                Subtitle = v.Subtitle,
                AudioUrl = v.AudioUrl,
                SortOrder = v.SortOrder,
                IsPublished = v.IsPublished
            }).ToList()
        };

        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.TotalCount = result.TotalCount;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        ViewData["Title"] = _localizer["CreateVoice"].Value;
        return View(new VoiceItemFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(VoiceItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        ViewData["Title"] = _localizer["CreateVoice"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _voices.CreateAsync(ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: created voice item {VoiceId}", id);
            TempData["Success"] = _localizer["VoiceCreated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        var item = await _voices.GetAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditVoice"].Value;
        return View(new VoiceItemFormViewModel
        {
            Id = item.Id,
            CustomerName = item.CustomerName,
            Subtitle = item.Subtitle,
            Description = item.Description,
            AudioUrl = item.AudioUrl,
            SortOrder = item.SortOrder,
            IsPublished = item.IsPublished
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, VoiceItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        ViewData["Title"] = _localizer["EditVoice"].Value;
        model.Id = id;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _voices.UpdateAsync(id, ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: updated voice item {VoiceId}", id);
            TempData["Success"] = _localizer["VoiceUpdated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Voices))
            return NotFound();

        await _voices.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted voice item {VoiceId}", id);
        TempData["Success"] = _localizer["VoiceDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static SaveVoiceItemCommand ToCommand(VoiceItemFormViewModel model) =>
        new(
            model.CustomerName.Trim(),
            string.IsNullOrWhiteSpace(model.Subtitle) ? null : model.Subtitle.Trim(),
            string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            model.AudioUrl.Trim(),
            model.SortOrder,
            model.IsPublished);

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }
}
