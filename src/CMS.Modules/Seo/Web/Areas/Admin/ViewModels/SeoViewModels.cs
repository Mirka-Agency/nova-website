using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Seo.Web.Areas.Admin.ViewModels;

public class SeoEditorFieldsViewModel
{
    public string ContentType { get; set; } = string.Empty;
    public Guid? ContentId { get; set; }

    [MaxLength(200)]
    public string? FocusKeyword { get; set; }

    public bool RobotsIndex { get; set; } = true;
    public bool RobotsFollow { get; set; } = true;

    [MaxLength(64)]
    public string? SchemaType { get; set; }

    public int? SeoScore { get; set; }

    /// <summary>Optional page title for SERP preview (from content meta/title).</summary>
    public string? PreviewTitle { get; set; }

    /// <summary>Optional meta description for SERP preview.</summary>
    public string? PreviewDescription { get; set; }

    /// <summary>Relative or absolute URL preview path.</summary>
    public string? PreviewUrl { get; set; }

    public List<SelectListItem> SchemaTypeOptions { get; set; } = [];
}

public class SeoSettingsViewModel
{
    [MaxLength(200)]
    public string? OrganizationName { get; set; }

    [MaxLength(1000)]
    public string? OrganizationUrl { get; set; }

    [MaxLength(1000)]
    public string? OrganizationLogoUrl { get; set; }

    [MaxLength(64)]
    public string? DefaultSchemaType { get; set; }

    [MaxLength(8000)]
    public string? RobotsTxtExtra { get; set; }

    [MaxLength(100)]
    public string? TwitterSiteHandle { get; set; }

    public bool EnableBrokenLinkChecks { get; set; }
    public bool SitemapEnabled { get; set; } = true;

    public List<SelectListItem> SchemaTypeOptions { get; set; } = [];
}

public class SeoRedirectIndexViewModel
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public List<SeoRedirectListItemViewModel> Items { get; set; } = [];
}

public class SeoRedirectListItemViewModel
{
    public Guid Id { get; set; }
    public string FromPath { get; set; } = string.Empty;
    public string ToUrl { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }
}

public class SeoRedirectFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FromPath { get; set; } = "/";

    [Required]
    [MaxLength(2000)]
    public string ToUrl { get; set; } = string.Empty;

    [Range(301, 302)]
    public int StatusCode { get; set; } = 301;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Note { get; set; }

    public List<SelectListItem> StatusCodeOptions { get; set; } = [];
}
