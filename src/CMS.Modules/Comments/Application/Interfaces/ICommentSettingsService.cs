using CMS.Modules.Comments.Application.Settings;

namespace CMS.Modules.Comments.Application.Interfaces;

public interface ICommentSettingsService
{
    Task<CommentSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateCommentSettingsCommand command, CancellationToken cancellationToken = default);
}
