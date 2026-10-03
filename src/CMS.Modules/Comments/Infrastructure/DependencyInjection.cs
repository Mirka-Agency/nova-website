using CMS.Modules.Comments.Application;
using CMS.Modules.Comments.Application.AntiSpam;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Infrastructure.Persistence;
using CMS.Modules.Comments.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Comments.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddCommentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddCommentsApplication();
        services.Configure<CommentsAntiSpamOptions>(configuration.GetSection(CommentsAntiSpamOptions.SectionName));

        services.AddDbContext<CommentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services.AddScoped<ICommentCaptchaValidator, CommentCaptchaValidator>();
        services.AddScoped<ICommentSettingsService, CommentSettingsService>();
        services.AddScoped<ICommentService, CommentService>();

        return services;
    }
}
