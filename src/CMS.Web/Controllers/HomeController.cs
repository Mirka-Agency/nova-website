using System.Diagnostics;
using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Application.TeamItems;
using CMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPublicTeamItemQuery _teamItems;
    private readonly IFormService _forms;
    private readonly IFeatureManager _features;

    public HomeController(
        IPublicTeamItemQuery teamItems,
        IFormService forms,
        IFeatureManager features)
    {
        _teamItems = teamItems;
        _forms = forms;
        _features = features;
    }

    public IActionResult Index()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    public IActionResult Privacy()
    {
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();
        return View();
    }

    [HttpGet("about")]
    public async Task<IActionResult> About(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "درباره ما";
        ViewData["NavActive"] = "about";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();

        IReadOnlyList<PublicTeamItemSummaryDto> team = Array.Empty<PublicTeamItemSummaryDto>();
        if (await _features.IsEnabledAsync(FeatureNames.Team))
        {
            var page = await _teamItems.ListPublishedPagedAsync(1, 4, cancellationToken);
            team = page.Items;
        }

        return View(team);
    }

    [HttpGet("contact")]
    public async Task<IActionResult> Contact(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "تماس با ما";
        ViewData["NavActive"] = "contact";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();

        var hasContactForm = false;
        if (await _features.IsEnabledAsync(FeatureNames.Forms))
        {
            var form = await _forms.GetPublicContractByKeyAsync("contact", cancellationToken);
            hasContactForm = form is not null && form.Fields.Count > 0;
        }

        ViewBag.HasContactForm = hasContactForm;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null, string? message = null)
    {
        var status = code ?? StatusCodes.Status500InternalServerError;
        Response.StatusCode = status;

        var (title, fallback) = status switch
        {
            StatusCodes.Status404NotFound => ("یافت نشد", "صفحه مورد نظر یافت نشد."),
            StatusCodes.Status400BadRequest => ("درخواست نامعتبر", "درخواست شما معتبر نیست."),
            _ => ("خطای سرور", "خطای غیرمنتظره‌ای رخ داد.")
        };

        return View(new ErrorViewModel
        {
            Title = title,
            Message = string.IsNullOrWhiteSpace(message) ? fallback : message,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
