using CMS.Application.Common.Features;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class CommerceReportsController : Controller
{
    private readonly ICommerceReportService _reports;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public CommerceReportsController(
        ICommerceReportService reports,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _reports = reports;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CommerceReports"].Value;
        var report = await _reports.GetAsync(cancellationToken);
        return View(new CommerceReportViewModel { Report = report });
    }
}
