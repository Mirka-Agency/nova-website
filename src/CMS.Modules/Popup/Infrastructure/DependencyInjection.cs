using CMS.Modules.Popup.Application;
using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Popup.Infrastructure.Persistence;
using CMS.Modules.Popup.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Popup.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddPopupModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddPopupApplication();

        services.AddDbContext<PopupDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IPopupService, PopupService>();
        services.AddScoped<IPublicPopupQuery, PublicPopupQuery>();

        return services;
    }
}
