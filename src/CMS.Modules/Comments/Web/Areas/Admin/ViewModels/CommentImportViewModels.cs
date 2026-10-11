using System.ComponentModel.DataAnnotations;
using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Comments.Web.Areas.Admin.ViewModels;

public sealed class CommentImportUploadViewModel
{
    [Required]
    public IFormFile? File { get; set; }
}

public sealed class CommentImportPreviewViewModel
{
    public string PreviewId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ReadyCount { get; set; }
    public int DuplicateCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int AmbiguousCount { get; set; }
    public int InvalidCount { get; set; }
    public bool OnlyNew { get; set; } = true;
    public IReadOnlyList<CommentImportPreviewRowViewModel> Rows { get; set; } = [];
    public IReadOnlyList<SelectListItem> TargetOptions { get; set; } = [];
}

public sealed class CommentImportPreviewRowViewModel
{
    public string ExternalId { get; set; } = string.Empty;
    public string? ExternalParentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string BodyPreview { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; }
    public CommentStatus Status { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public string NormalizedPath { get; set; } = string.Empty;
    public CommentImportRowDisposition Disposition { get; set; }
    public CommentTargetType? MatchedTargetType { get; set; }
    public Guid? MatchedTargetId { get; set; }
    public string? MatchedTargetTitle { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Manual override value: "{targetType}:{targetId}".</summary>
    public string? ManualTargetKey { get; set; }
}

public sealed class CommentImportConfirmFormViewModel
{
    public string PreviewId { get; set; } = string.Empty;
    public bool OnlyNew { get; set; } = true;
    public List<CommentImportManualOverrideFormItem> Overrides { get; set; } = [];
}

public sealed class CommentImportManualOverrideFormItem
{
    public string ExternalId { get; set; } = string.Empty;
    public string? ManualTargetKey { get; set; }
}

public sealed class CommentImportResultViewModel
{
    public string PreviewId { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ImportedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int UnmatchedCount { get; set; }
    public int FailedCount { get; set; }
    public bool Queued { get; set; }
    public IReadOnlyList<CommentImportResultRowViewModel> Rows { get; set; } = [];
}

public sealed class CommentImportResultRowViewModel
{
    public string ExternalId { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? TargetTitle { get; set; }
}
