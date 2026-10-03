using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Seo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSeoApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
