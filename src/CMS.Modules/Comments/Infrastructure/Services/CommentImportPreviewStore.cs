using CMS.Modules.Comments.Application.Import;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CMS.Modules.Comments.Infrastructure.Services;

public sealed class CommentImportPreviewStore : ICommentImportPreviewStore
{
    private readonly IMemoryCache _cache;
    private readonly CommentImportOptions _options;

    public CommentImportPreviewStore(IMemoryCache cache, IOptions<CommentImportOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public Task SaveAsync(CommentImportPreview preview, CancellationToken cancellationToken = default)
    {
        var ttl = TimeSpan.FromMinutes(Math.Clamp(_options.PreviewTtlMinutes, 5, 24 * 60));
        _cache.Set(CacheKey(preview.PreviewId), preview, ttl);
        return Task.CompletedTask;
    }

    public Task<CommentImportPreview?> GetAsync(string previewId, CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(CacheKey(previewId), out CommentImportPreview? preview);
        return Task.FromResult(preview);
    }

    public Task RemoveAsync(string previewId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey(previewId));
        return Task.CompletedTask;
    }

    private static string CacheKey(string previewId) => "comments:import-preview:" + previewId;
}
