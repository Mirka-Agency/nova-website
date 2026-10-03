using CMS.Application.Seo;
using CMS.Modules.Seo.Application;
using CMS.Modules.Seo.Application.Interfaces;
using CMS.Modules.Seo.Infrastructure.Persistence;
using CMS.Modules.Seo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Seo.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddSeoModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddSeoApplication();

        services.AddDbContext<SeoDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<ISeoDocumentService, SeoDocumentService>();
        services.AddScoped<ISeoRedirectService, SeoRedirectService>();
        services.AddScoped<ISeoSiteSettingsService, SeoSiteSettingsService>();
        services.AddScoped<ISeoLinkSuggestionService, SeoLinkSuggestionService>();
        services.AddScoped<IBrokenLinkChecker, BrokenLinkChecker>();
        services.AddScoped<IRobotsTxtBuilder, RobotsTxtBuilder>();

        services.AddHttpClient(BrokenLinkChecker.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MirkaSeoBrokenLinkChecker/1.0");
        });

        return services;
    }
}
