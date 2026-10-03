using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CMS.Modules.Team.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
