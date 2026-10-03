using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Voices.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddVoicesApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
