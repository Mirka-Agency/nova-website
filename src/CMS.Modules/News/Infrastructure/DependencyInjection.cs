using CMS.Application.Seo;
using CMS.Modules.News.Application;
using CMS.Modules.News.Application.Interfaces;
using CMS.Modules.News.Infrastructure.Persistence;
using CMS.Modules.News.Infrastructure.Seo;
using CMS.Modules.News.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.News.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddNewsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddNewsApplication();

        services.AddDbContext<NewsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPublicArticleQuery, PublicArticleQuery>();
        services.AddScoped<ISitemapUrlProvider, NewsSitemapUrlProvider>();
        services.AddScoped<IInternalLinkCandidateProvider, NewsInternalLinkCandidateProvider>();

        return services;
    }
}
