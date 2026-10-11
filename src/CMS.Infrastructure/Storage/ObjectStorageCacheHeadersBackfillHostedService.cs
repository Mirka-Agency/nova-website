using CMS.Application.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Storage;

/// <summary>
/// Optional one-shot backfill of Cache-Control on existing S3 objects.
/// Enable with <c>Storage__S3__BackfillCacheHeadersOnStartup=true</c>, then disable after success.
/// </summary>
public sealed class ObjectStorageCacheHeadersBackfillHostedService : IHostedService
{
    private readonly IObjectStorage _storage;
    private readonly S3ObjectStorageOptions _options;
    private readonly ILogger<ObjectStorageCacheHeadersBackfillHostedService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private Task? _running;

    public ObjectStorageCacheHeadersBackfillHostedService(
        IObjectStorage storage,
        IOptions<S3ObjectStorageOptions> options,
        ILogger<ObjectStorageCacheHeadersBackfillHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _storage = storage;
        _options = options.Value;
        _logger = logger;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.BackfillCacheHeadersOnStartup)
            return Task.CompletedTask;

        _running = RunAsync(_lifetime.ApplicationStopping);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_running is null)
            return;

        try
        {
            await _running.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Let the host finish wiring before listing the bucket.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _logger.LogInformation(
            "Starting S3 Cache-Control backfill (Storage__S3__BackfillCacheHeadersOnStartup=true). Disable this flag after it succeeds.");

        try
        {
            var result = await _storage.BackfillCacheHeadersAsync(cancellationToken);
            _logger.LogInformation(
                "S3 Cache-Control backfill complete: examined={Examined}, updated={Updated}, skipped={Skipped}, failed={Failed}. Set Storage__S3__BackfillCacheHeadersOnStartup=false.",
                result.Examined, result.Updated, result.Skipped, result.Failed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("S3 Cache-Control backfill canceled during shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S3 Cache-Control backfill failed");
        }
    }
}
