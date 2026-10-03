using CMS.Modules.Forms.Application;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Infrastructure.Persistence;
using CMS.Modules.Forms.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Forms.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddFormsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddFormsApplication();
        services.Configure<CMS.Modules.Forms.Application.AntiSpam.FormsAntiSpamOptions>(
            configuration.GetSection(CMS.Modules.Forms.Application.AntiSpam.FormsAntiSpamOptions.SectionName));
        services.AddHttpClient("forms-webhook", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddHttpClient("forms-antispam", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(8);
        });

        services.AddDbContext<FormsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<IFormService, FormService>();
        services.AddScoped<ISubmissionService, SubmissionService>();

        return services;
    }
}
