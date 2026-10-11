namespace CMS.Modules.Comments.Application.Import;

public interface ICommentImportService
{
    Task<CommentImportPreview> BuildPreviewAsync(Stream xmlStream, CancellationToken cancellationToken = default);

    Task<CommentImportPreview?> GetPreviewAsync(string previewId, CancellationToken cancellationToken = default);

    Task<CommentImportResult> ConfirmAsync(
        CommentImportConfirmCommand command,
        CancellationToken cancellationToken = default);

    byte[] BuildCsvReport(CommentImportResult result);

    byte[] GetSampleXml();
}
