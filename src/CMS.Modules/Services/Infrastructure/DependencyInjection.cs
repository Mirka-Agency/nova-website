using CMS.Application.Seo;
using CMS.Modules.Services.Application;
using CMS.Modules.Services.Application.Interfaces;
using CMS.Modules.Services.Infrastructure.Persistence;
using CMS.Modules.Services.Infrastructure.Seo;
using CMS.Modules.Services.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Services.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddServicesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddServicesApplication();

        services.AddDbContext<ServicesDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IServiceItemService, ServiceItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPublicServiceItemQuery, PublicServiceItemQuery>();
        services.AddScoped<ISitemapUrlProvider, ServicesSitemapUrlProvider>();

        return services;
    }
}
