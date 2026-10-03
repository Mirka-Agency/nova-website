using CMS.Application.Auth;

namespace CMS.Web.Middleware;

/// <summary>
/// Prevents response-caching middleware from storing/serving HTML personalized by the staff admin bar.
/// Must run before <c>UseResponseCaching</c>.
/// </summary>
public sealed class StaffResponseCacheBypassMiddleware
{
    private readonly RequestDelegate _next;

    public StaffResponseCacheBypassMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(StaffAuthDefaults.CookieName))
        {
            // ResponseCachingPolicyProvider skips cache lookup when the request has Cache-Control: no-cache.
            context.Request.Headers.CacheControl = "no-cache";
        }

        await _next(context);
    }
}

public static class StaffResponseCacheBypassMiddlewareExtensions
{
    public static IApplicationBuilder UseStaffResponseCacheBypass(this IApplicationBuilder app)
        => app.UseMiddleware<StaffResponseCacheBypassMiddleware>();
}
