namespace CMS.Web.Middleware;

/// <summary>
/// Long-lived browser cache for versioned static assets (CSS/JS/images/fonts).
/// Safe with <c>asp-append-version</c>: when file content changes the URL hash changes,
/// so clients fetch the new asset while unchanged files stay in cache.
/// </summary>
public sealed class StaticAssetsCacheMiddleware
{
    private static readonly HashSet<string> CachedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css", ".js", ".mjs", ".map",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ico", ".avif",
        ".woff", ".woff2", ".ttf", ".eot", ".otf",
    };

    private const string CacheControlValue = "public,max-age=31536000,immutable";

    private readonly RequestDelegate _next;
    private readonly bool _enabled;

    public StaticAssetsCacheMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        // Keep Development on short/no-cache from MapStaticAssets for easier local edits.
        _enabled = !environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_enabled && IsCacheableStaticAsset(context.Request.Path))
        {
            context.Response.OnStarting(static state =>
            {
                var http = (HttpContext)state!;
                if (http.Response.StatusCode is >= 200 and < 300)
                    http.Response.Headers.CacheControl = CacheControlValue;
                return Task.CompletedTask;
            }, context);
        }

        await _next(context);
    }

    private static bool IsCacheableStaticAsset(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value))
            return false;

        var ext = Path.GetExtension(value);
        return ext.Length > 0 && CachedExtensions.Contains(ext);
    }
}

public static class StaticAssetsCacheMiddlewareExtensions
{
    public static IApplicationBuilder UseStaticAssetsCacheHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<StaticAssetsCacheMiddleware>();
}
