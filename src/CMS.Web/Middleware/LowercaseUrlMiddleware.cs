namespace CMS.Web.Middleware;

/// <summary>
/// 301-redirects GET/HEAD requests to a single canonical public path form:
/// lowercase segments and a trailing slash (except root, APIs, admin, and file-like paths).
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

        var needsLower = HasUpperAscii(path);
        var needsSlash = path.Length > 1 && !path.EndsWith('/');

        if (!needsLower && !needsSlash)
            return false;

        canonical = needsLower ? path.ToLowerInvariant() : path;
        if (needsSlash)
            canonical += "/";

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
        var lastSlash = path.LastIndexOf('/');
        var lastSegment = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;
        if (lastSegment.Contains('.', StringComparison.Ordinal))
            return true;

        return false;
    }

    private static bool HasUpperAscii(string path)
    {
        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c is >= 'A' and <= 'Z')
                return true;
        }

        return false;
    }
}

public static class LowercaseUrlMiddlewareExtensions
{
    public static IApplicationBuilder UseLowercaseUrlsRedirect(this IApplicationBuilder app)
        => app.UseMiddleware<LowercaseUrlMiddleware>();
}
