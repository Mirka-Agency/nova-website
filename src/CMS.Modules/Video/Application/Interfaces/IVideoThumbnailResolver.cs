namespace CMS.Modules.Video.Application.Interfaces;

/// <summary>Resolves a thumbnail URL for a video link, including Aparat API lookup.</summary>
public interface IVideoThumbnailResolver
{
    Task<string?> ResolveAsync(string? videoUrl, CancellationToken cancellationToken = default);
}
