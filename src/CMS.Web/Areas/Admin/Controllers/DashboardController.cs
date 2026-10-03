using CMS.Application.Common.Features;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Identity;
using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Media.Application.Interfaces;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly IFeatureManager _features;
    private readonly IAuthorizationService _authorization;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPostService _posts;
    private readonly IArticleService _articles;
    private readonly IServiceItemService _serviceItems;
    private readonly IVideoItemService _videoItems;
    private readonly ITeamItemService _teamItems;
    private readonly IProductService _products;
    private readonly IOrderService _orders;
    private readonly IFormService _forms;
    private readonly ISubmissionService _submissions;
    private readonly IPopupService _popups;
    private readonly ISeoRedirectService _seoRedirects;
    private readonly IMediaLibraryService _media;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public DashboardController(
        IFeatureManager features,
        IAuthorizationService authorization,
        UserManager<ApplicationUser> userManager,
        IPostService posts,
        IArticleService articles,
        IServiceItemService serviceItems,
        IVideoItemService videoItems,
        ITeamItemService teamItems,
        IProductService products,
        IOrderService orders,
        IFormService forms,
        ISubmissionService submissions,
        IPopupService popups,
        ISeoRedirectService seoRedirects,
        IMediaLibraryService media,
        IStringLocalizer<AdminShared> localizer)
    {
        _features = features;
        _authorization = authorization;
        _userManager = userManager;
        _posts = posts;
        _articles = articles;
        _serviceItems = serviceItems;
        _videoItems = videoItems;
        _teamItems = teamItems;
        _products = products;
        _orders = orders;
        _forms = forms;
        _submissions = submissions;
        _popups = popups;
        _seoRedirects = seoRedirects;
        _media = media;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var cards = new List<DashboardStatCardViewModel>();

        if (await _features.IsEnabledAsync(FeatureNames.Blog)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewBlog)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Posts"],
                await _posts.CountAsync(cancellationToken),
                Url.Action("Index", "Posts", new { area = "Admin" }),
                _localizer["ManagePosts"],
                "fas fa-newspaper"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.News)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewNews)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["NewsArticles"],
                await _articles.CountAsync(cancellationToken),
                Url.Action("Index", "NewsArticles", new { area = "Admin" }),
                _localizer["ManageNewsArticles"],
                "fas fa-bullhorn"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Services)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewServices)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["ServiceItems"],
                await _serviceItems.CountAsync(cancellationToken),
                Url.Action("Index", "ServiceItems", new { area = "Admin" }),
                _localizer["ManageServiceItems"],
                "fas fa-concierge-bell"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Video)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewVideo)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["VideoItems"],
                await _videoItems.CountAsync(cancellationToken),
                Url.Action("Index", "VideoItems", new { area = "Admin" }),
                _localizer["ManageVideoItems"],
                "fas fa-video"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Team)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewTeam)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["TeamItems"],
                await _teamItems.CountAsync(cancellationToken),
                Url.Action("Index", "TeamItems", new { area = "Admin" }),
                _localizer["ManageTeamItems"],
                "fas fa-users"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Shop)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewShop)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Products"],
                await _products.CountAsync(cancellationToken),
                Url.Action("Index", "Products", new { area = "Admin" }),
                _localizer["ManageProducts"],
                "fas fa-box"));
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Orders"],
                await _orders.CountAsync(cancellationToken),
                Url.Action("Index", "Orders", new { area = "Admin" }),
                _localizer["ManageOrders"],
                "fas fa-shopping-bag"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Forms)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewForms)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Forms"],
                await _forms.CountAsync(cancellationToken),
                Url.Action("Index", "Forms", new { area = "Admin" }),
                _localizer["ManageForms"],
                "fas fa-clipboard-list"));
            cards.Add(new DashboardStatCardViewModel(
                _localizer["FormSubmissions"],
                await _submissions.CountAsync(cancellationToken),
                Url.Action("Index", "FormSubmissions", new { area = "Admin" }),
                _localizer["ViewSubmissions"],
                "fas fa-inbox"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Popup)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewPopup)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Popups"],
                await _popups.CountAsync(cancellationToken),
                Url.Action("Index", "Popups", new { area = "Admin" }),
                _localizer["ManagePopups"],
                "fas fa-window-restore"));
        }

        if (await _features.IsEnabledAsync(FeatureNames.Seo)
            && (await _authorization.AuthorizeAsync(User, AuthPolicies.ViewSeo)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["SeoRedirects"],
                await _seoRedirects.CountAsync(cancellationToken),
                Url.Action("Index", "SeoRedirects", new { area = "Admin" }),
                _localizer["ManageSeo"],
                "fas fa-search"));
        }

        if ((await _authorization.AuthorizeAsync(User, AuthPolicies.ViewMedia)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["MediaLibrary"],
                await _media.CountAsync(cancellationToken),
                Url.Action("Index", "Media", new { area = "Admin" }),
                _localizer["ManageMedia"],
                "fas fa-images"));
        }

        if ((await _authorization.AuthorizeAsync(User, AuthPolicies.AdminOnly)).Succeeded)
        {
            cards.Add(new DashboardStatCardViewModel(
                _localizer["Users"],
                await _userManager.Users.AsNoTracking().CountAsync(cancellationToken),
                Url.Action("Index", "Users", new { area = "Admin" }),
                _localizer["ManageUsers"],
                "fas fa-users"));
        }

        return View(new DashboardViewModel(cards));
    }
}
