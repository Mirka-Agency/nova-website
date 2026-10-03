using CMS.Application.Common.Features;
using CMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace CMS.Infrastructure.Features;

public sealed class DatabaseFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private static readonly string[] KnownFeatures =
    [
        FeatureNames.Blog,
        FeatureNames.News,
        FeatureNames.Services,
        FeatureNames.Video,
        FeatureNames.Team,
        FeatureNames.Honors,
        FeatureNames.Shop,
        FeatureNames.Forms,
        FeatureNames.Comments,
        FeatureNames.Popup,
        FeatureNames.Seo
    ];

    public const string ToggleCacheKey = "cms:feature-toggles";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(45);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public DatabaseFeatureDefinitionProvider(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
    {
        var toggles = await GetToggleMapAsync();
        if (toggles.TryGetValue(featureName, out var enabled))
            return CreateDefinition(featureName, enabled);

        var configured = _configuration.GetValue<bool?>($"FeatureManagement:{featureName}");
        if (configured.HasValue)
            return CreateDefinition(featureName, configured.Value);

        return null;
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        var toggles = await GetToggleMapAsync();

        foreach (var name in KnownFeatures)
        {
            if (toggles.TryGetValue(name, out var enabled))
            {
                yield return CreateDefinition(name, enabled);
                continue;
            }

            var configured = _configuration.GetValue<bool?>($"FeatureManagement:{name}") ?? false;
            yield return CreateDefinition(name, configured);
        }
    }

    private async Task<IReadOnlyDictionary<string, bool>> GetToggleMapAsync()
    {
        if (_cache.TryGetValue(ToggleCacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var map = await db.FeatureToggles.AsNoTracking()
            .ToDictionaryAsync(t => t.Name, t => t.Enabled);

        _cache.Set(ToggleCacheKey, (IReadOnlyDictionary<string, bool>)map, CacheDuration);
        return map;
    }

    private static FeatureDefinition CreateDefinition(string name, bool enabled) =>
        new()
        {
            Name = name,
            EnabledFor = enabled
                ? [new FeatureFilterConfiguration { Name = "AlwaysOn" }]
                : Array.Empty<FeatureFilterConfiguration>()
        };
}
