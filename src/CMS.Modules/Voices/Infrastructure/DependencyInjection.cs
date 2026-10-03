using CMS.Modules.Voices.Application;
using CMS.Modules.Voices.Application.Interfaces;
using CMS.Modules.Voices.Infrastructure.Persistence;
using CMS.Modules.Voices.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Voices.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddVoicesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddVoicesApplication();

        services.AddDbContext<VoicesDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IVoiceItemService, VoiceItemService>();
        services.AddScoped<IPublicVoiceItemQuery, PublicVoiceItemQuery>();

        return services;
    }
}
