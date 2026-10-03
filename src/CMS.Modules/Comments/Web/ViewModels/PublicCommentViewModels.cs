using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Application.Settings;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Web.ViewModels;

public sealed class CommentsSectionViewModel
{
    public CommentTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string TargetTitle { get; set; } = string.Empty;
    public CommentSettingsDto Settings { get; set; } = null!;
    public IReadOnlyList<PublicCommentDto> Comments { get; set; } = [];
    public SubmitCommentFormViewModel Form { get; set; } = new();
    public bool CanSubmit { get; set; }
    public bool RequiresLogin { get; set; }
    public string? CaptchaSiteKey { get; set; }
    public string? ReturnUrl { get; set; }
}

public sealed class SubmitCommentFormViewModel
{
    public CommentTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string TargetTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string? AuthorPhone { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? CaptchaToken { get; set; }
    public string? ReturnUrl { get; set; }
}
