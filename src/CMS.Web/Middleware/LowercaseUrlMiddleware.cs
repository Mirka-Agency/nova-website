using CMS.Application.Seo;
using Microsoft.AspNetCore.Diagnostics;

namespace CMS.Web.Middleware;

/// <summary>
/// 301-redirects GET/HEAD requests to a single canonical public path form:
/// lowercase segments, collapsed duplicate slashes, and a trailing slash
/// (except root, APIs, admin, and file-like paths) — in one hop.
/// </summary>
public sealed class LowercaseUrlMiddleware
{
    private readonly RequestDelegate _next;

    public LowercaseUrlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // Status-code re-execute must keep the original browser URL (e.g. show 404 in place).
        if (context.Features.Get<IStatusCodeReExecuteFeature>() is not null)
            return _next(context);

        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) && TryCanonicalize(path, out var canonical))
            {
                var location = canonical + context.Request.QueryString.Value;
                context.Response.Redirect(location, permanent: true);
                return Task.CompletedTask;
            }
        }

        return _next(context);
    }

    internal static bool TryCanonicalize(string path, out string canonical)
    {
        canonical = path;

        if (ShouldSkip(path))
            return false;

        // One 301 covers case, middle/trailing "//", and missing trailing slash together.
        var normalized = CanonicalUrlBuilder.NormalizeStructuralPath(path);
        if (string.Equals(path, normalized, StringComparison.Ordinal))
            return false;

        canonical = normalized;
        return true;
    }

    private static bool ShouldSkip(string path)
    {
        if (path.Equals("/", StringComparison.Ordinal))
            return true;

        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Static / file-like paths: robots.txt, sitemap.xml, assets with extensions.
        // Use the last non-empty segment so /file.txt/// and //file.txt are still files.
        var trimmed = path.TrimEnd('/');
        while (trimmed.Contains("//", StringComparison.Ordinal))
            trimmed = trimmed.Replace("//", "/", StringComparison.Ordinal);

        if (trimmed.Length == 0)
            return false;

        var lastSlash = trimmed.LastIndexOf('/');
        var lastSegment = lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
        if (lastSegment.Contains('.', StringComparison.Ordinal))
            return true;

        return false;
    }
}

public static class LowercaseUrlMiddlewareExtensions
{
    public static IApplicationBuilder UseLowercaseUrlsRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<LowercaseUrlMiddleware>();
}
