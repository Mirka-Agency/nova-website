using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Shop.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddShopApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
