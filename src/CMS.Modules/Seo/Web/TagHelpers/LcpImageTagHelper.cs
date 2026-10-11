using CMS.Application.Seo;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CMS.Modules.Seo.Web.TagHelpers;

/// <summary>
/// Marks the page LCP <c>&lt;img&gt;</c>: <c>loading="eager"</c>, <c>fetchpriority="high"</c>,
/// and registers a single head preload via <see cref="LcpImageHints"/>.
/// Usage: <c>&lt;img lcp src="..." width="..." height="..." alt="..." /&gt;</c>
/// </summary>
[HtmlTargetElement("img", Attributes = LcpAttributeName)]
public sealed class LcpImageTagHelper : TagHelper
{
    private const string LcpAttributeName = "lcp";

    /// <summary>Run after URL resolution so <c>src</c> is final.</summary>
    public override int Order => 1000;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <summary>When true (default), apply LCP hints. Set <c>lcp="false"</c> to opt out.</summary>
    [HtmlAttributeName(LcpAttributeName)]
    public bool Lcp { get; set; } = true;

    /// <summary>Optional responsive srcset mirrored to preload <c>imagesrcset</c>.</summary>
    [HtmlAttributeName("lcp-srcset")]
    public string? LcpSrcSet { get; set; }

    /// <summary>Optional sizes mirrored to preload <c>imagesizes</c>.</summary>
    [HtmlAttributeName("lcp-sizes")]
    public string? LcpSizes { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.Attributes.RemoveAll(LcpAttributeName);
        output.Attributes.RemoveAll("lcp-srcset");
        output.Attributes.RemoveAll("lcp-sizes");

        if (!Lcp)
            return;

        output.Attributes.SetAttribute("loading", "eager");
        output.Attributes.SetAttribute("fetchpriority", "high");

        if (!output.Attributes.ContainsName("decoding"))
            output.Attributes.SetAttribute("decoding", "async");

        if (!string.IsNullOrWhiteSpace(LcpSrcSet) && !output.Attributes.ContainsName("srcset"))
            output.Attributes.SetAttribute("srcset", LcpSrcSet.Trim());

        if (!string.IsNullOrWhiteSpace(LcpSizes) && !output.Attributes.ContainsName("sizes"))
            output.Attributes.SetAttribute("sizes", LcpSizes.Trim());

        var src = TagHelperAttributeValue.Read(output, "src");
        LcpImageHints.TryRegister(ViewContext.ViewData, src, LcpSrcSet, LcpSizes);
    }
}
