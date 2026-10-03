namespace CMS.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    /// <summary>
    /// Baseline CSP for same-origin MVC + known captcha/CDN script hosts.
    /// Inline scripts are still allowed ('unsafe-inline') because Admin/public
    /// Razor views use small inline blocks; tighten further when those move to files.
    /// </summary>
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "img-src 'self' data: blob: https:; " +
        "media-src 'self' https: blob:; " +
        "font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://challenges.cloudflare.com https://www.google.com https://www.gstatic.com https://js.hcaptcha.com; " +
        "connect-src 'self' https:; " +
        // Captcha widgets + video embeds (Aparat / YouTube / Vimeo iframes on public + admin).
        "frame-src https://challenges.cloudflare.com https://www.google.com https://www.gstatic.com https://recaptcha.google.com https://newassets.hcaptcha.com https://js.hcaptcha.com https://www.aparat.com https://aparat.com https://www.youtube.com https://www.youtube-nocookie.com https://youtube.com https://player.vimeo.com";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "0";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
