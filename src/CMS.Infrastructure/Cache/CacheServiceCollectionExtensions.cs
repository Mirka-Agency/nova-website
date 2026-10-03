using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace CMS.Infrastructure.Cache;

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddCmsCacheAndDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>();
        var redisConnection = options?.RedisConnectionString?.Trim();

        services.AddMemoryCache();

        if (environment.IsProduction() && string.IsNullOrWhiteSpace(redisConnection))
        {
            throw new InvalidOperationException(
                "Cache:RedisConnectionString is required in Production so Data Protection keys " +
                "and distributed cache are shared across instances. Set Cache__RedisConnectionString.");
        }

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            var redis = ConnectionMultiplexer.Connect(redisConnection);
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);
            services.AddDataProtection()
                .PersistKeysToStackExchangeRedis(redis, "CMS-DataProtection-Keys");
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddDataProtection();
        }

        return services;
    }
}
