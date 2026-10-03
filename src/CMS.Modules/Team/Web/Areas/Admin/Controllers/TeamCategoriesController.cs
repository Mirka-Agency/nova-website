using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Team.Application.Categories;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Web.Areas.Admin.ViewModels;
using CMS.Modules.Team.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Team.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewTeam")]
public class TeamCategoriesController : Controller
{
    private readonly ICategoryService _categories;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<TeamAdmin> _localizer;
    private readonly ILogger<TeamCategoriesController> _logger;

    public TeamCategoriesController(
        ICategoryService categories,
        IFeatureManager features,
        IStringLocalizer<TeamAdmin> localizer,
        ILogger<TeamCategoriesController> logger)
    {
        _categories = categories;
        _features = features;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["Categories"].Value;
        var items = await _categories.ListAsync(cancellationToken);
        var mapped = items.Select(c => new CategoryListItemViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            TeamItemCount = c.TeamItemCount,
            ImageUrl = c.ImageUrl
        }).ToList();
        var paged = PageSlice.FromList(mapped, page);
        PageSlice.ApplyToViewBag(ViewBag, paged);
        return View(paged.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCategory"].Value;
        return View(new CategoryFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCategory"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _categories.CreateAsync(
                new SaveCategoryCommand(model.Name, model.Slug, model.Description, NullIfWhiteSpace(model.ImageUrl)),
                cancellationToken);
            _logger.LogInformation("Admin action: created blog category {CategoryId}", id);
            TempData["Success"] = _localizer["CategoryCreated"].Value;
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
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        var category = await _categories.GetAsync(id, cancellationToken);
        if (category is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditCategory"].Value;
        return View(new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CategoryFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Team))
            return NotFound();

        ViewData["Title"] = _localizer["EditCategory"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _categories.UpdateAsync(
                id,
                new SaveCategoryCommand(model.Name, model.Slug, model.Description, NullIfWhiteSpace(model.ImageUrl)),
                cancellationToken);
            _logger.LogInformation("Admin action: updated service category {CategoryId}", id);
            TempData["Success"] = _localizer["CategoryUpdated"].Value;
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

        await _categories.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted service category {CategoryId}", id);
        TempData["Success"] = _localizer["CategoryDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void AddValidationErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
        {
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
        }
    }
}
