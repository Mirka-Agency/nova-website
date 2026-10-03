using CMS.Application.Seo;
using CMS.Modules.Blog.Application;
using CMS.Modules.Blog.Application.Interfaces;
using CMS.Modules.Blog.Infrastructure.Persistence;
using CMS.Modules.Blog.Infrastructure.Seo;
using CMS.Modules.Blog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Blog.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddBlogModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddBlogApplication();

        services.AddDbContext<BlogDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPublicPostQuery, PublicPostQuery>();
        services.AddScoped<ISitemapUrlProvider, BlogSitemapUrlProvider>();
        services.AddScoped<IInternalLinkCandidateProvider, BlogInternalLinkCandidateProvider>();

        return services;
    }
}
