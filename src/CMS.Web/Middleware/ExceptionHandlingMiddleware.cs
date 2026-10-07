using System.Text.Json;
using CMS.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Handled {ExceptionType} for {Method} {Path}: {Message}",
                exception.GetType().Name, context.Request.Method, context.Request.Path, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        if (WantsJson(context))
        {
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Extensions =
                {
                    ["traceId"] = context.TraceIdentifier
                }
            };

            if (errors is not null)
            {
                problem.Extensions["errors"] = errors;
            }

            if (_environment.IsDevelopment() && statusCode >= 500)
            {
                problem.Extensions["exception"] = exception.ToString();
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
            return;
        }

        var isAdmin = context.Request.Path.StartsWithSegments("/admin");
        var basePath = (statusCode, isAdmin) switch
        {
            (StatusCodes.Status404NotFound, true) => "/admin/error/notfound",
            (StatusCodes.Status400BadRequest, true) => "/admin/error/badrequest",
            (_, true) => "/admin/error/servererror",
            (StatusCodes.Status404NotFound, false) => "/home/error",
            (StatusCodes.Status400BadRequest, false) => "/home/error",
            _ => "/home/error"
        };

        var location = $"{basePath}?code={statusCode}&message={Uri.EscapeDataString(detail)}";
        context.Response.Redirect(location);
    }

    private static (int StatusCode, string Title, string Detail, IReadOnlyDictionary<string, string[]>? Errors)
        MapException(Exception exception) =>
        exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "یافت نشد",
                string.IsNullOrWhiteSpace(notFound.Message) ? "مورد درخواستی یافت نشد." : notFound.Message,
                null),
            ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "خطای اعتبارسنجی",
                string.IsNullOrWhiteSpace(validation.Message) ? "داده‌های ارسالی معتبر نیست." : validation.Message,
                validation.Errors.Count > 0 ? validation.Errors : null),
            DomainException domain => (
                StatusCodes.Status400BadRequest,
                "خطای دامنه",
                string.IsNullOrWhiteSpace(domain.Message) ? "عملیات مجاز نیست." : domain.Message,
                null),
            OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "درخواست لغو شد",
                "آپلود یا درخواست قطع شد. اتصال شبکه و تنظیمات S3 را بررسی کنید.",
                null),
            InvalidOperationException invalid when invalid.Message.Contains("S3", StringComparison.OrdinalIgnoreCase)
                || invalid.Message.Contains("آپلود", StringComparison.OrdinalIgnoreCase) => (
                StatusCodes.Status502BadGateway,
                "خطای ذخیره‌سازی",
                invalid.Message,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "خطای سرور",
                "خطای غیرمنتظره‌ای رخ داد.",
                null)
        };

    private static bool WantsJson(HttpContext context)
    {
        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
               || accept.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase)
               || context.Request.Path.StartsWithSegments("/api");
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
