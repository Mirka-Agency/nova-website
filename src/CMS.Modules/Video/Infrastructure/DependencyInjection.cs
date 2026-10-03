using CMS.Application.Seo;
using CMS.Modules.Video.Application;
using CMS.Modules.Video.Application.Interfaces;
using CMS.Modules.Video.Infrastructure.Persistence;
using CMS.Modules.Video.Infrastructure.Seo;
using CMS.Modules.Video.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Video.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddVideoModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddVideoApplication();

        services.AddDbContext<VideoDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IVideoItemService, VideoItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPublicVideoItemQuery, PublicVideoItemQuery>();
        services.AddScoped<ISitemapUrlProvider, VideosSitemapUrlProvider>();

        return services;
    }
}
