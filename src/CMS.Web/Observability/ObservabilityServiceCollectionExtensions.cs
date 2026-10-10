using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CMS.Web.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public const string ReadyTag = "ready";
    public const string ReadyTokenHeaderName = "X-Health-Token";

    public static IServiceCollection AddCmsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var builder = services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag]);

        var redis = configuration["Cache:RedisConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            builder.AddRedis(
                redis,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag]);
        }

        return services;
    }

    public static IServiceCollection AddCmsOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Testing"))
            return services;

        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "CMS.Web";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0")
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName)
                ]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else if (environment.IsDevelopment())
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                else if (environment.IsDevelopment())
                    metrics.AddConsoleExporter();
            });

        return services;
    }

    public static WebApplication MapCmsHealthEndpoints(this WebApplication app)
    {
        // Liveness: process is up (no dependency checks). CapRover / Docker default path.
        app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => false
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        var ready = app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag)
            })
            .AllowAnonymous()
            .DisableRateLimiting();

        var readyToken = app.Configuration["HealthChecks:ReadyToken"]?.Trim();
        if (!string.IsNullOrEmpty(readyToken))
        {
            ready.AddEndpointFilter(async (context, next) =>
            {
                if (!context.HttpContext.Request.Headers.TryGetValue(ReadyTokenHeaderName, out var provided)
                    || !string.Equals(provided.ToString(), readyToken, StringComparison.Ordinal))
                {
                    return Results.Unauthorized();
                }

                return await next(context);
            });
        }

        return app;
    }
}
