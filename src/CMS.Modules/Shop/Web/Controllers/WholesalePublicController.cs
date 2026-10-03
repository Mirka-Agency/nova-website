using CMS.Application.Common.Features;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;
using System.Security.Claims;

namespace CMS.Modules.Shop.Web.Controllers;

[Route("shop/wholesale")]
public class WholesalePublicController : Controller
{
    private readonly IWholesaleService _wholesale;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopPublic> _localizer;

    public WholesalePublicController(
        IWholesaleService wholesale,
        IFeatureManager features,
        IStringLocalizer<ShopPublic> localizer)
    {
        _wholesale = wholesale;
        _features = features;
        _localizer = localizer;
    }

    [HttpGet("apply")]
    public async Task<IActionResult> Apply()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["WholesaleApply"].Value;
        return View(new WholesaleApplyFormViewModel());
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(WholesaleApplyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["WholesaleApply"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await _wholesale.SubmitAsync(new SubmitWholesaleRequestCommand(
                userId,
                model.CompanyName,
                model.BusinessInfo ?? string.Empty,
                model.ContactName,
                model.Phone,
                model.Email,
                model.Address,
                null), cancellationToken);
            TempData["Success"] = _localizer["WholesaleSubmitted"].Value;
            return View("ApplyResult");
        }
        catch (ValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
