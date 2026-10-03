using System.ComponentModel.DataAnnotations;
using CMS.Modules.Media.Application.Imaging;
using Microsoft.AspNetCore.Http;

namespace CMS.Modules.Media.Web.Areas.Admin.ViewModels;

public sealed class MediaListItemViewModel
{
    public Guid Id { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? AltText { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public string SizeDisplay { get; init; } = string.Empty;
    public string PublicUrl { get; init; } = string.Empty;
    public string ThumbnailPublicUrl { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class MediaUploadViewModel
{
    [Required(ErrorMessage = "انتخاب فایل الزامی است.")]
    public IFormFile? File { get; set; }

    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? AltText { get; set; }

    /// <summary>Optional max edge for the WebP display file. Empty = leave default processing.</summary>
    [Range(MediaOptimizeOptions.MinDimension, MediaOptimizeOptions.MaxDimension)]
    public int? MaxWidth { get; set; }

    /// <summary>Optional WebP quality. Empty = leave default processing.</summary>
    [Range(MediaOptimizeOptions.MinQuality, MediaOptimizeOptions.MaxQuality)]
    public int? Quality { get; set; }
}

public sealed class MediaMetadataFormViewModel
{
    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? AltText { get; set; }

    [StringLength(1000)]
    public string? Caption { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }
}
