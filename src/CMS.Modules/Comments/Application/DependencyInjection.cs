using System.Reflection;
using CMS.Modules.Comments.Application.AntiSpam;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Comments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCommentsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<ICommentsAntiSpamCredentials, CommentsAntiSpamCredentials>();
        return services;
    }
}
