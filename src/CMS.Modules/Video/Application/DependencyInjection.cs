using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Video.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVideoApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
