using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public ReviewsController(IReviewService reviews, IFeatureManager features, IStringLocalizer<ShopAdmin> localizer)
    {
        _reviews = reviews;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, bool? approved = null, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Reviews"].Value;
        ViewBag.Approved = approved;

        var result = await _reviews.ListPagedAsync(new PagedRequest { Page = page }, approved, cancellationToken);
        PageSlice.ApplyToViewBag(ViewBag, result);
        return View(result.Items);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id, string? adminResponse, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _reviews.ModerateAsync(id, new ModerateReviewCommand(true, adminResponse), cancellationToken);
            TempData["Success"] = _localizer["ReviewApproved"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _reviews.ModerateAsync(id, new ModerateReviewCommand(false, null), cancellationToken);
            TempData["Success"] = _localizer["ReviewRejected"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
