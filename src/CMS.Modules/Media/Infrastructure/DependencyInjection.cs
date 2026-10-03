using CMS.Modules.Media.Application;
using CMS.Modules.Media.Application.Imaging;
using CMS.Modules.Media.Application.Interfaces;
using CMS.Modules.Media.Infrastructure.Imaging;
using CMS.Modules.Media.Infrastructure.Persistence;
using CMS.Modules.Media.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Media.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddMediaApplication();

        services.AddDbContext<MediaDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddSingleton<IMediaImageOptimizer, ImageSharpMediaOptimizer>();
        services.AddScoped<IMediaLibraryService, MediaLibraryService>();

        return services;
    }
}
