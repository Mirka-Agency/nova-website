using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Honors.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Honors.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewHonors")]
public class HonorItemsController : Controller
{
    private readonly IHonorItemService _honors;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<HonorsAdmin> _localizer;
    private readonly ILogger<HonorItemsController> _logger;

    public HonorItemsController(
        IHonorItemService honors,
        IFeatureManager features,
        IStringLocalizer<HonorsAdmin> localizer,
        ILogger<HonorItemsController> logger)
    {
        _honors = honors;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        ViewData["Title"] = _localizer["HonorItems"].Value;
        ViewBag.Search = q;
        ViewBag.HasActiveFilters = published.HasValue;

        var result = await _honors.ListPagedAsync(
            new HonorItemListRequest { Page = page, Search = q, IsPublished = published },
            cancellationToken);

        var model = new HonorItemIndexViewModel
        {
            Search = q,
            IsPublished = published,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            Items = result.Items.Select(h => new HonorItemListItemViewModel
            {
                Id = h.Id,
                Title = h.Title,
                ImageUrl = h.ImageUrl,
                AltText = h.AltText,
                SortOrder = h.SortOrder,
                IsPublished = h.IsPublished
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
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        ViewData["Title"] = _localizer["CreateHonor"].Value;
        return View(new HonorItemFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(HonorItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        ViewData["Title"] = _localizer["CreateHonor"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _honors.CreateAsync(ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: created honor item {HonorId}", id);
            TempData["Success"] = _localizer["HonorCreated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        var item = await _honors.GetAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditHonor"].Value;
        return View(new HonorItemFormViewModel
        {
            Id = item.Id,
            Title = item.Title,
            ImageUrl = item.ImageUrl,
            AltText = item.AltText,
            SortOrder = item.SortOrder,
            IsPublished = item.IsPublished
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, HonorItemFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        ViewData["Title"] = _localizer["EditHonor"].Value;
        model.Id = id;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _honors.UpdateAsync(id, ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: updated honor item {HonorId}", id);
            TempData["Success"] = _localizer["HonorUpdated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Honors))
            return NotFound();

        await _honors.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted honor item {HonorId}", id);
        TempData["Success"] = _localizer["HonorDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static SaveHonorItemCommand ToCommand(HonorItemFormViewModel model) =>
        new(
            model.Title,
            model.ImageUrl.Trim(),
            string.IsNullOrWhiteSpace(model.AltText) ? null : model.AltText.Trim(),
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
