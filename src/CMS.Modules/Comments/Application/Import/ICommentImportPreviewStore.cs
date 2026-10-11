namespace CMS.Modules.Comments.Application.Import;

public interface ICommentImportPreviewStore
{
    Task SaveAsync(CommentImportPreview preview, CancellationToken cancellationToken = default);
    Task<CommentImportPreview?> GetAsync(string previewId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string previewId, CancellationToken cancellationToken = default);
}
