using CMS.Application.Settings;
using CMS.Infrastructure.Auth;

namespace CMS.Web.Middleware;

public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISiteSettingsService settings)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/admin")
            || path.StartsWithSegments("/api")
            || path.StartsWithSegments("/home/error")
            || path.StartsWithSegments("/sitemap.xml")
            || path.StartsWithSegments("/sitemaps")
            || path.StartsWithSegments("/robots.txt")
            || path.StartsWithSegments("/css")
            || path.StartsWithSegments("/js")
            || path.StartsWithSegments("/lib")
            || path.StartsWithSegments("/site")
            || path.StartsWithSegments("/template")
            || path.StartsWithSegments("/favicon"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true
            && context.User.IsInRole(AuthRoles.Admin))
        {
            await _next(context);
            return;
        }

        var site = await settings.GetAsync(context.RequestAborted);
        if (!site.MaintenanceMode)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "3600";
        context.Response.ContentType = "text/html; charset=utf-8";

        var message = string.IsNullOrWhiteSpace(site.MaintenanceMessage)
            ? "سایت موقتاً در دسترس نیست. لطفاً بعداً مراجعه کنید."
            : site.MaintenanceMessage;
        var brand = System.Net.WebUtility.HtmlEncode(site.SiteName);
        var body = System.Net.WebUtility.HtmlEncode(message);

        await context.Response.WriteAsync(
            $$"""
             <!DOCTYPE html>
             <html lang="fa" dir="rtl">
             <head>
               <meta charset="utf-8" />
               <meta name="viewport" content="width=device-width, initial-scale=1" />
               <title>{{brand}}</title>
               <style>
                 body{font-family:Tahoma,sans-serif;background:#f4f7f8;color:#12202a;display:grid;place-items:center;min-height:100vh;margin:0;padding:1.5rem;}
                 main{max-width:32rem;background:#fff;border:1px solid #d7e0e6;border-radius:.75rem;padding:2rem;line-height:1.8;}
                 h1{margin:0 0 .75rem;font-size:1.35rem;}
                 p{margin:0;color:#5a6b76;}
               </style>
             </head>
             <body><main><h1>{{brand}}</h1><p>{{body}}</p></main></body>
             </html>
             """);
    }
}

public static class MaintenanceModeMiddlewareExtensions
{
    public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder app)
        => app.UseMiddleware<MaintenanceModeMiddleware>();
}
