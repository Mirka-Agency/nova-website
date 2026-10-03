using CMS.Modules.Honors.Application;
using CMS.Modules.Honors.Application.Interfaces;
using CMS.Modules.Honors.Infrastructure.Persistence;
using CMS.Modules.Honors.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Honors.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddHonorsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddHonorsApplication();

        services.AddDbContext<HonorsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IHonorItemService, HonorItemService>();
        services.AddScoped<IPublicHonorItemQuery, PublicHonorItemQuery>();

        return services;
    }
}
