using CMS.Modules.Seo.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Seo.Infrastructure.Middleware;

public sealed class SeoRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public SeoRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            if (!path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                var redirects = context.RequestServices.GetService<ISeoRedirectService>();
                if (redirects is not null)
                {
                    var match = await redirects.ResolveAsync(path, context.RequestAborted);
                    if (match is not null)
                    {
                        context.Response.StatusCode = match.StatusCode;
                        context.Response.Headers.Location = match.ToUrl;
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}

public static class SeoRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseSeoRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<SeoRedirectMiddleware>();
}
