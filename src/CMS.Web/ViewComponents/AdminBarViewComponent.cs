using System.Security.Claims;
using CMS.Application.Admin;
using CMS.Application.Common.Features;
using CMS.Infrastructure.Auth;
using CMS.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Web.ViewComponents;

public sealed class AdminBarViewComponent : ViewComponent
{
    private readonly IAuthorizationService _authorization;
    private readonly IFeatureManager _features;

    public AdminBarViewComponent(IAuthorizationService authorization, IFeatureManager features)
    {
        _authorization = authorization;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var auth = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        var staff = auth.Principal;
        if (!IsStaff(staff))
            return Content(string.Empty);

        // Avoid caching personalized HTML that includes this bar.
        HttpContext.Response.Headers.CacheControl = "private, no-store";

        var editContext = ViewContext.ViewData[AdminEditContext.ViewDataKey] as AdminEditContext;
        AdminBarLink? editLink = null;
        if (editContext is not null
            && await IsAuthorizedAsync(staff, editContext.RequiredPolicy))
        {
            editLink = new AdminBarLink(
                NormalizeEditLabel(editContext.Label),
                editContext.Controller,
                editContext.Action,
                editContext.Id,
                editContext.Area,
                "fa-edit");
        }

        var manageLinks = await BuildManageLinksAsync(staff);
        var createLinks = await BuildCreateLinksAsync(staff);
        var canManageSettings = await IsAuthorizedAsync(staff, AuthPolicies.AdminOnly);

        return View(new AdminBarViewModel
        {
            DisplayName = ResolveDisplayName(staff),
            EditLink = editLink,
            CanManageSettings = canManageSettings,
            ManageLinks = manageLinks,
            CreateLinks = createLinks
        });
    }

    private async Task<IReadOnlyList<AdminBarLink>> BuildManageLinksAsync(ClaimsPrincipal staff)
    {
        var links = new List<AdminBarLink>(10);

        await TryAddAsync(links, staff, FeatureNames.Blog, AuthPolicies.ViewBlog, "نوشته‌ها", "Posts", "Index", "fa-newspaper");
        await TryAddAsync(links, staff, FeatureNames.News, AuthPolicies.ViewNews, "مقالات تخصصی", "NewsArticles", "Index", "fa-newspaper");
        await TryAddAsync(links, staff, FeatureNames.Services, AuthPolicies.ViewServices, "خدمات", "ServiceItems", "Index", "fa-concierge-bell");
        await TryAddAsync(links, staff, FeatureNames.Video, AuthPolicies.ViewVideo, "ویدیوها", "VideoItems", "Index", "fa-video");
        await TryAddAsync(links, staff, FeatureNames.Team, AuthPolicies.ViewTeam, "تیم", "TeamItems", "Index", "fa-users");
        await TryAddAsync(links, staff, FeatureNames.Honors, AuthPolicies.ViewHonors, "افتخارات", "HonorItems", "Index", "fa-award");
        await TryAddAsync(links, staff, FeatureNames.Voices, AuthPolicies.ViewVoices, "صدای رضایت", "VoiceItems", "Index", "fa-microphone-alt");
        await TryAddAsync(links, staff, FeatureNames.Shop, AuthPolicies.ViewShop, "محصولات", "Products", "Index", "fa-box");
        await TryAddAsync(links, staff, FeatureNames.Forms, AuthPolicies.ViewForms, "فرم‌ها", "Forms", "Index", "fa-clipboard-list");
        await TryAddAsync(links, staff, FeatureNames.Popup, AuthPolicies.ViewPopup, "پاپ‌آپ‌ها", "Popups", "Index", "fa-window-restore");
        await TryAddAsync(links, staff, FeatureNames.Seo, AuthPolicies.ViewSeo, "سئو", "SeoSettings", "Index", "fa-search");
        await TryAddAsync(links, staff, FeatureNames.Comments, AuthPolicies.ViewComments, "نظرات", "Comments", "Index", "fa-comments");

        if (await IsAuthorizedAsync(staff, AuthPolicies.ViewMedia))
            links.Add(new AdminBarLink("رسانه", "Media", "Index", Icon: "fa-images"));

        return links;
    }

    private async Task<IReadOnlyList<AdminBarLink>> BuildCreateLinksAsync(ClaimsPrincipal staff)
    {
        var links = new List<AdminBarLink>(8);

        await TryAddAsync(links, staff, FeatureNames.Blog, AuthPolicies.ManageBlog, "نوشته جدید", "Posts", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.News, AuthPolicies.ManageNews, "مقاله تخصصی جدید", "NewsArticles", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Services, AuthPolicies.ManageServices, "خدمت جدید", "ServiceItems", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Video, AuthPolicies.ManageVideo, "ویدیو جدید", "VideoItems", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Team, AuthPolicies.ManageTeam, "عضو تیم جدید", "TeamItems", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Honors, AuthPolicies.ManageHonors, "افتخار جدید", "HonorItems", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Voices, AuthPolicies.ManageVoices, "صدای جدید", "VoiceItems", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Shop, AuthPolicies.ManageShop, "محصول جدید", "Products", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Forms, AuthPolicies.ManageForms, "فرم جدید", "Forms", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Popup, AuthPolicies.ManagePopup, "پاپ‌آپ جدید", "Popups", "Create", "fa-plus");
        await TryAddAsync(links, staff, FeatureNames.Seo, AuthPolicies.ManageSeo, "ریدایرکت جدید", "SeoRedirects", "Create", "fa-plus");

        return links;
    }

    private async Task TryAddAsync(
        List<AdminBarLink> links,
        ClaimsPrincipal staff,
        string feature,
        string policy,
        string label,
        string controller,
        string action,
        string icon)
    {
        if (!await _features.IsEnabledAsync(feature))
            return;
        if (!await IsAuthorizedAsync(staff, policy))
            return;

        links.Add(new AdminBarLink(label, controller, action, Icon: icon));
    }

    private async Task<bool> IsAuthorizedAsync(ClaimsPrincipal staff, string? policy)
    {
        if (string.IsNullOrWhiteSpace(policy))
            return true;

        var result = await _authorization.AuthorizeAsync(staff, policy);
        return result.Succeeded;
    }

    private static bool IsStaff(ClaimsPrincipal user) =>
        user.IsInRole(AuthRoles.Admin)
        || user.IsInRole(AuthRoles.Editor)
        || user.IsInRole(AuthRoles.ShopManager)
        || user.IsInRole(AuthRoles.Viewer);

    private static string ResolveDisplayName(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email);
        var name = user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name)
            && !string.Equals(name, email, StringComparison.OrdinalIgnoreCase))
            return name.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            return email.Trim();

        return name?.Trim() ?? "کاربر";
    }

    /// <summary>Keep short, action-first labels for the primary edit button.</summary>
    private static string NormalizeEditLabel(string label)
    {
        var trimmed = label.Trim();
        if (trimmed.StartsWith("ویرایش", StringComparison.Ordinal))
            return trimmed;

        if (trimmed.StartsWith("مدیریت", StringComparison.Ordinal))
            return trimmed;

        return "ویرایش این صفحه";
    }
}
