using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Domain.Entities;
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
                        if (match.StatusCode == 200)
                        {
                            if (TryApplyRewrite(context, match.ToUrl))
                            {
                                await _next(context);
                                return;
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = match.StatusCode;
                            context.Response.Headers.Location = match.ToUrl;
                            return;
                        }
                    }
                }
            }
        }

        await _next(context);
    }

    private static bool TryApplyRewrite(HttpContext context, string toUrl)
    {
        if (string.IsNullOrWhiteSpace(toUrl))
            return false;

        var trimmed = toUrl.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            return false;

        var parts = trimmed.Split('?', 2);
        var targetPath = SeoRedirect.NormalizePath(parts[0]);
        if (!targetPath.StartsWith('/'))
            return false;

        var currentPath = SeoRedirect.NormalizePath(
            context.Request.Path.HasValue ? context.Request.Path.Value : "/");
        if (string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
            return false;

        context.Request.Path = targetPath;
        if (parts.Length > 1)
            context.Request.QueryString = QueryString.FromUriComponent("?" + parts[1]);
        return true;
    }
}

public static class SeoRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseSeoRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<SeoRedirectMiddleware>();
}
