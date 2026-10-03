using CMS.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CMS.Web.Security;

/// <summary>
/// Blocks non-safe HTTP methods for admin module controllers unless the user satisfies the module's Manage* policy.
/// Controllers use View* at class level so Viewer can browse lists; this filter enforces write access.
/// </summary>
public sealed class AdminMutationAuthorizationFilter : IAsyncAuthorizationFilter
{
    private static readonly Dictionary<string, string> ManagePolicyByController =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Posts"] = AuthPolicies.ManageBlog,
            ["Categories"] = AuthPolicies.ManageBlog,
            ["BlogMedia"] = AuthPolicies.ManageBlog,
            ["BlogPostsApi"] = AuthPolicies.ManageBlog,
            ["NewsArticles"] = AuthPolicies.ManageNews,
            ["NewsCategories"] = AuthPolicies.ManageNews,
            ["NewsMedia"] = AuthPolicies.ManageNews,
            ["NewsArticlesApi"] = AuthPolicies.ManageNews,
            ["ServiceItems"] = AuthPolicies.ManageServices,
            ["ServiceCategories"] = AuthPolicies.ManageServices,
            ["ServicesMedia"] = AuthPolicies.ManageServices,
            ["ServiceItemsApi"] = AuthPolicies.ManageServices,
            ["VideoItems"] = AuthPolicies.ManageVideo,
            ["VideoCategories"] = AuthPolicies.ManageVideo,
            ["VideosMedia"] = AuthPolicies.ManageVideo,
            ["VideoItemsApi"] = AuthPolicies.ManageVideo,
            ["TeamItems"] = AuthPolicies.ManageTeam,
            ["TeamCategories"] = AuthPolicies.ManageTeam,
            ["TeamsMedia"] = AuthPolicies.ManageTeam,
            ["TeamItemsApi"] = AuthPolicies.ManageTeam,
            ["ShopHome"] = AuthPolicies.ManageShop,
            ["Products"] = AuthPolicies.ManageShop,
            ["ProductSpecifications"] = AuthPolicies.ManageShop,
            ["ProductVariations"] = AuthPolicies.ManageShop,
            ["ShopMedia"] = AuthPolicies.ManageShop,
            ["ShopCategories"] = AuthPolicies.ManageShop,
            ["Orders"] = AuthPolicies.ManageShop,
            ["ShopSettings"] = AuthPolicies.ManageShop,
            ["Brands"] = AuthPolicies.ManageShop,
            ["Attributes"] = AuthPolicies.ManageShop,
            ["CustomerGroups"] = AuthPolicies.ManageShop,
            ["PriceRules"] = AuthPolicies.ManageShop,
            ["Wholesale"] = AuthPolicies.ManageShop,
            ["Coupons"] = AuthPolicies.ManageShop,
            ["ShippingMethods"] = AuthPolicies.ManageShop,
            ["PaymentProviders"] = AuthPolicies.ManageShop,
            ["Reviews"] = AuthPolicies.ManageShop,
            ["CommerceReports"] = AuthPolicies.ManageShop,
            ["Forms"] = AuthPolicies.ManageForms,
            ["FormSubmissions"] = AuthPolicies.ManageFormSubmissions,
            ["Media"] = AuthPolicies.ManageMedia,
            ["Comments"] = AuthPolicies.ManageComments,
            ["CommentSettings"] = AuthPolicies.ManageComments,
            ["Popups"] = AuthPolicies.ManagePopup,
            ["SeoSettings"] = AuthPolicies.ManageSeo,
            ["SeoRedirects"] = AuthPolicies.ManageSeo,
            ["SeoDocumentsApi"] = AuthPolicies.ManageSeo,
            ["SeoToolsApi"] = AuthPolicies.ManageSeo,
            ["Users"] = AuthPolicies.AdminOnly,
            ["Features"] = AuthPolicies.AdminOnly,
            ["Settings"] = AuthPolicies.AdminOnly,
        };

    private readonly IAuthorizationService _authorization;

    public AdminMutationAuthorizationFilter(IAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (!string.Equals(context.RouteData.Values["area"]?.ToString(), "Admin", StringComparison.OrdinalIgnoreCase))
            return;

        var method = http.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
            return;

        var controller = context.RouteData.Values["controller"]?.ToString();
        if (string.IsNullOrWhiteSpace(controller)
            || !ManagePolicyByController.TryGetValue(controller, out var policy))
        {
            return;
        }

        var result = await _authorization.AuthorizeAsync(http.User, policy);
        if (!result.Succeeded)
            context.Result = new ForbidResult();
    }
}
