using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Infrastructure.Persistence;

/// <summary>
/// Phase 3 backfill seeder — no-op after SubmissionValues drop (DataJson is the only store).
/// </summary>
public static class FormEnginePhase3Seeder
{
    public static Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Forms.FormEnginePhase3Seeder");
        logger.LogDebug("FormEnginePhase3Seeder skipped (SubmissionValues dropped; DataJson is canonical).");
        return Task.CompletedTask;
    }
}
