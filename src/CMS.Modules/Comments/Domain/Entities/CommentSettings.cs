using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Domain.Entities;

public class CommentSettings : BaseEntity
{
    private CommentSettings()
    {
    }

    public bool AllowAnonymous { get; private set; } = true;
    public bool EnableOnBlog { get; private set; } = true;
    public bool EnableOnEvents { get; private set; } = true;
    public bool EnableOnProducts { get; private set; } = true;
    public bool EnableOnProductCategories { get; private set; } = true;
    public bool ShowEmail { get; private set; }
    public bool RequireEmail { get; private set; }
    public bool ShowPhone { get; private set; }
    public bool RequirePhone { get; private set; }
    public bool EnableCaptcha { get; private set; } = true;
    public CommentCaptchaProvider CaptchaProvider { get; private set; } = CommentCaptchaProvider.Recaptcha;

    public static CommentSettings CreateDefault() => new();

    public bool IsEnabledFor(CommentTargetType targetType) =>
        targetType switch
        {
            CommentTargetType.BlogPost => EnableOnBlog,
            CommentTargetType.Event => EnableOnEvents,
            CommentTargetType.Product => EnableOnProducts,
            CommentTargetType.ProductCategory => EnableOnProductCategories,
            _ => false
        };

    public void Update(
        bool allowAnonymous,
        bool enableOnBlog,
        bool enableOnEvents,
        bool enableOnProducts,
        bool enableOnProductCategories,
        bool showEmail,
        bool requireEmail,
        bool showPhone,
        bool requirePhone,
        bool enableCaptcha,
        CommentCaptchaProvider captchaProvider)
    {
        if (!Enum.IsDefined(captchaProvider))
            throw new DomainException("نوع کپچا نامعتبر است.");
        if (enableCaptcha && captchaProvider == CommentCaptchaProvider.None)
            throw new DomainException("برای فعال‌سازی کپچا، یک ارائه‌دهنده انتخاب کنید.");

        if (requireEmail && !showEmail)
            throw new DomainException("برای اجباری بودن ایمیل، نمایش فیلد ایمیل باید فعال باشد.");
        if (requirePhone && !showPhone)
            throw new DomainException("برای اجباری بودن تلفن، نمایش فیلد تلفن باید فعال باشد.");

        AllowAnonymous = allowAnonymous;
        EnableOnBlog = enableOnBlog;
        EnableOnEvents = enableOnEvents;
        EnableOnProducts = enableOnProducts;
        EnableOnProductCategories = enableOnProductCategories;
        ShowEmail = showEmail;
        RequireEmail = requireEmail;
        ShowPhone = showPhone;
        RequirePhone = requirePhone;
        EnableCaptcha = enableCaptcha;
        CaptchaProvider = enableCaptcha ? captchaProvider : CommentCaptchaProvider.None;
        Touch();
    }
}
