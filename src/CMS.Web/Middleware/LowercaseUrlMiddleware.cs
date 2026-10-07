namespace CMS.Web.Middleware;

/// <summary>
/// 301-redirects GET/HEAD requests whose path contains uppercase letters to the lowercase path.
/// Keeps a single canonical URL form (ASP.NET routing itself is case-insensitive).
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
            if (!string.IsNullOrEmpty(path) && HasUpperAscii(path))
            {
                var lower = path.ToLowerInvariant();
                var location = lower + context.Request.QueryString.Value;
                context.Response.Redirect(location, permanent: true);
                return Task.CompletedTask;
            }
        }

        return _next(context);
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
