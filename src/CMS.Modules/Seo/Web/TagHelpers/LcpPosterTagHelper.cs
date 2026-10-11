using CMS.Application.Seo;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CMS.Modules.Seo.Web.TagHelpers;

/// <summary>
/// Registers the video <c>poster</c> as the page LCP image for head preload.
/// Usage: <c>&lt;video lcp-poster poster="@coverUrl" ...&gt;</c>
/// </summary>
[HtmlTargetElement("video", Attributes = LcpPosterAttributeName)]
public sealed class LcpPosterTagHelper : TagHelper
{
    private const string LcpPosterAttributeName = "lcp-poster";

    /// <summary>Run after URL resolution so <c>poster</c> is final.</summary>
    public override int Order => 1000;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    [HtmlAttributeName(LcpPosterAttributeName)]
    public bool LcpPoster { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll(LcpPosterAttributeName);

        if (!LcpPoster)
            return;

        var poster = TagHelperAttributeValue.Read(output, "poster");
        if (string.IsNullOrWhiteSpace(poster))
            return;

        LcpImageHints.TryRegister(ViewContext.ViewData, poster);
    }
}
