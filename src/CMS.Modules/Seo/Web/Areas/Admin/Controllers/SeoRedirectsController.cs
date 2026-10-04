using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Application.Redirects;
using CMS.Modules.Seo.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Seo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewSeo")]
public class SeoRedirectsController : Controller
{
    private readonly ISeoRedirectService _redirects;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<SeoAdmin> _localizer;
    private readonly ILogger<SeoRedirectsController> _logger;

    public SeoRedirectsController(
        ISeoRedirectService redirects,
        IFeatureManager features,
        IStringLocalizer<SeoAdmin> localizer,
        ILogger<SeoRedirectsController> logger)
    {
        _redirects = redirects;
        _features = features;
        _localizer = localizer;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? q = null, bool? active = null, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["Redirects"].Value;
        var result = await _redirects.ListPagedAsync(
            new SeoRedirectListRequest { Page = page, Search = q, IsActive = active },
            cancellationToken);

        ViewBag.Page = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        ViewBag.Search = q;
        ViewBag.IsActive = active;

        var model = new SeoRedirectIndexViewModel
        {
            Search = q,
            IsActive = active,
            Items = result.Items.Select(r => new SeoRedirectListItemViewModel
            {
                Id = r.Id,
                FromPath = r.FromPath,
                ToUrl = r.ToUrl,
                StatusCode = r.StatusCode,
                IsActive = r.IsActive,
                Note = r.Note
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["CreateRedirect"].Value;
        return View(PrepareForm(new SeoRedirectFormViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SeoRedirectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["CreateRedirect"].Value;
        model = PrepareForm(model);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var id = await _redirects.CreateAsync(ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: created SEO redirect {RedirectId}", id);
            TempData["Success"] = _localizer["RedirectCreated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        var item = await _redirects.GetAsync(id, cancellationToken);
        if (item is null)
            return NotFound();

        ViewData["Title"] = _localizer["EditRedirect"].Value;
        return View(PrepareForm(new SeoRedirectFormViewModel
        {
            Id = item.Id,
            FromPath = item.FromPath,
            ToUrl = item.ToUrl,
            StatusCode = item.StatusCode,
            IsActive = item.IsActive,
            Note = item.Note
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, SeoRedirectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        ViewData["Title"] = _localizer["EditRedirect"].Value;
        model.Id = id;
        model = PrepareForm(model);

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _redirects.UpdateAsync(id, ToCommand(model), cancellationToken);
            _logger.LogInformation("Admin action: updated SEO redirect {RedirectId}", id);
            TempData["Success"] = _localizer["RedirectUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        await _redirects.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Admin action: deleted SEO redirect {RedirectId}", id);
        TempData["Success"] = _localizer["RedirectDeleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Seo))
            return NotFound();

        await _redirects.SetActiveAsync(id, isActive, cancellationToken);
        TempData["Success"] = isActive ? _localizer["RedirectActivated"].Value : _localizer["RedirectDeactivated"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static SaveSeoRedirectCommand ToCommand(SeoRedirectFormViewModel model) =>
        new(model.FromPath, model.ToUrl, model.StatusCode, model.IsActive, model.Note);

    private SeoRedirectFormViewModel PrepareForm(SeoRedirectFormViewModel model)
    {
        model.StatusCodeOptions =
        [
            new SelectListItem(_localizer["StatusCode200"].Value, "200", model.StatusCode == 200),
            new SelectListItem(_localizer["StatusCode301"].Value, "301", model.StatusCode == 301),
            new SelectListItem(_localizer["StatusCode302"].Value, "302", model.StatusCode == 302)
        ];
        return model;
    }
}
