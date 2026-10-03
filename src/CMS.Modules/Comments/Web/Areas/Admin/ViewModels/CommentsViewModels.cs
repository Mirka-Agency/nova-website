using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Web.Areas.Admin.ViewModels;

public sealed class CommentListFilterViewModel
{
    public CommentTargetType? TargetType { get; set; }
    public CommentStatus? Status { get; set; }
    public string? Search { get; set; }
    public string? FromLocal { get; set; }
    public string? ToLocal { get; set; }
    public string? Sort { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search)
        || TargetType.HasValue
        || Status.HasValue
        || !string.IsNullOrWhiteSpace(FromLocal)
        || !string.IsNullOrWhiteSpace(ToLocal)
        || !string.IsNullOrWhiteSpace(Sort);

    public object ToRouteValues() => new
    {
        targetType = TargetType.HasValue ? (int?)TargetType.Value : null,
        status = Status.HasValue ? (int?)Status.Value : null,
        q = Search,
        from = FromLocal,
        to = ToLocal,
        sort = Sort
    };
}

public sealed class CommentIndexViewModel
{
    public CommentListFilterViewModel Filter { get; init; } = new();
    public IReadOnlyList<CommentListItemViewModel> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class CommentListItemViewModel
{
    public Guid Id { get; init; }
    public CommentTargetType TargetType { get; init; }
    public string TargetTitle { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorEmail { get; init; }
    public string? AuthorPhone { get; init; }
    public string Body { get; init; } = string.Empty;
    public CommentStatus Status { get; init; }
    public string PublishedAtLocal { get; init; } = string.Empty;
}

public sealed class CommentEditViewModel
{
    public Guid Id { get; set; }
    public CommentTargetType TargetType { get; set; }
    public string TargetTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string? AuthorPhone { get; set; }
    public CommentStatus Status { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PublishedAtLocal { get; set; }
}

public sealed class CommentSettingsFormViewModel
{
    public bool AllowAnonymous { get; set; } = true;
    public bool EnableOnBlog { get; set; } = true;
    public bool EnableOnEvents { get; set; } = true;
    public bool EnableOnProducts { get; set; } = true;
    public bool EnableOnProductCategories { get; set; } = true;
    public bool ShowEmail { get; set; }
    public bool RequireEmail { get; set; }
    public bool ShowPhone { get; set; }
    public bool RequirePhone { get; set; }
    public bool EnableCaptcha { get; set; }
    public CommentCaptchaProvider CaptchaProvider { get; set; }
}
