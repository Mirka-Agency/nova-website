using CMS.Application.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace CMS.Web.Middleware;

/// <summary>
/// For non-admin requests, prefers the customer auth cookie as <see cref="HttpContext.User"/>.
/// </summary>
public sealed class CustomerPrincipalMiddleware
{
    private readonly RequestDelegate _next;

    public CustomerPrincipalMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Keep Identity admin principal for Admin UI and admin JSON APIs
        // (e.g. /api/v1/admin/blog/posts used by post autosave).
        var path = context.Request.Path;
        if (path.StartsWithSegments("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var result = await context.AuthenticateAsync(CustomerAuthDefaults.AuthenticationScheme);
        if (result.Succeeded && result.Principal is not null)
            context.User = result.Principal;

        await _next(context);
    }
}
