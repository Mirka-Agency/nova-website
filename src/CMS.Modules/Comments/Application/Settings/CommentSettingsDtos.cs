using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Settings;

public sealed record CommentSettingsDto(
    bool AllowAnonymous,
    bool EnableOnBlog,
    bool EnableOnEvents,
    bool EnableOnProducts,
    bool EnableOnProductCategories,
    bool ShowEmail,
    bool RequireEmail,
    bool ShowPhone,
    bool RequirePhone,
    bool EnableCaptcha,
    CommentCaptchaProvider CaptchaProvider)
{
    public bool IsEnabledFor(CommentTargetType targetType) =>
        targetType switch
        {
            CommentTargetType.BlogPost => EnableOnBlog,
            CommentTargetType.Event => EnableOnEvents,
            CommentTargetType.Product => EnableOnProducts,
            CommentTargetType.ProductCategory => EnableOnProductCategories,
            _ => false
        };
}

public sealed record UpdateCommentSettingsCommand(
    bool AllowAnonymous,
    bool EnableOnBlog,
    bool EnableOnEvents,
    bool EnableOnProducts,
    bool EnableOnProductCategories,
    bool ShowEmail,
    bool RequireEmail,
    bool ShowPhone,
    bool RequirePhone,
    bool EnableCaptcha,
    CommentCaptchaProvider CaptchaProvider);
