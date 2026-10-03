using CMS.Application.Seo;
using CMS.Modules.Team.Application;
using CMS.Modules.Team.Application.Interfaces;
using CMS.Modules.Team.Infrastructure.Persistence;
using CMS.Modules.Team.Infrastructure.Seo;
using CMS.Modules.Team.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Team.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddTeamModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddTeamApplication();

        services.AddDbContext<TeamDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<ITeamItemService, TeamItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IPublicTeamItemQuery, PublicTeamItemQuery>();
        services.AddScoped<ISitemapUrlProvider, TeamsSitemapUrlProvider>();

        return services;
    }
}
