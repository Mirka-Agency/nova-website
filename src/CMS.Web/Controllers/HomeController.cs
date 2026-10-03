using System.Diagnostics;
using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.News.Application.Articles;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Domain.Enums;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Application.ServiceItems;
using CMS.Modules.Honors.Application.HonorItems;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Application.TeamItems;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Application.VoiceItems;
using CMS.Web.Models;
using CMS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IPublicTeamItemQuery _teamItems;
    private readonly IPublicHonorItemQuery _honors;
    private readonly IPublicVoiceItemQuery _voices;
    private readonly IPublicServiceItemQuery _services;
    private readonly IPublicArticleQuery _articles;
    private readonly IFormService _forms;
    private readonly IFeatureManager _features;

    public HomeController(
        IPublicTeamItemQuery teamItems,
        IPublicHonorItemQuery honors,
        IPublicVoiceItemQuery voices,
        IPublicServiceItemQuery services,
        IPublicArticleQuery articles,
        IFormService forms,
        IFeatureManager features)
    {
        _teamItems = teamItems;
        _honors = honors;
        _voices = voices;
        _services = services;
        _articles = articles;
        _forms = forms;
        _features = features;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "خانه";
        ViewData["NavActive"] = "home";
        ViewData[AdminEditContext.ViewDataKey] = AdminEditContext.SiteSettings();

        IReadOnlyList<PublicServiceItemSummaryDto> services = Array.Empty<PublicServiceItemSummaryDto>();
        IReadOnlyList<PublicTeamItemSummaryDto> team = Array.Empty<PublicTeamItemSummaryDto>();
        IReadOnlyList<PublicArticleSummaryDto> articles = Array.Empty<PublicArticleSummaryDto>();
        IReadOnlyList<PublicVoiceItemDto> voices = Array.Empty<PublicVoiceItemDto>();

        if (await _features.IsEnabledAsync(FeatureNames.Services))
        {
            var page = await _services.ListPublishedPagedAsync(1, 4, cancellationToken);
            services = page.Items;
        }

        if (await _features.IsEnabledAsync(FeatureNames.Team))
        {
            var page = await _teamItems.ListPublishedPagedAsync(1, 6, cancellationToken);
            team = page.Items;
        }

        if (await _features.IsEnabledAsync(FeatureNames.News))
        {
            var page = await _articles.ListPublishedByKindPagedAsync(ArticleKind.News, 1, 3, cancellationToken);
            articles = page.Items;
        }

        if (await _features.IsEnabledAsync(FeatureNames.Voices))
            voices = await _voices.ListPublishedAsync(cancellationToken);

        return View(new HomeIndexViewModel
        {
            Services = services,
            Team = team,
            Articles = articles,
            Voices = voices
        });
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
        IReadOnlyList<PublicHonorItemDto> honors = Array.Empty<PublicHonorItemDto>();

        if (await _features.IsEnabledAsync(FeatureNames.Team))
        {
            var page = await _teamItems.ListPublishedPagedAsync(1, 4, cancellationToken);
            team = page.Items;
        }

        if (await _features.IsEnabledAsync(FeatureNames.Honors))
            honors = await _honors.ListPublishedAsync(cancellationToken);

        return View(new AboutPageViewModel
        {
            Team = team,
            Honors = honors
        });
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
