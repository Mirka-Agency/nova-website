using CMS.Application.Security;
using Ganss.Xss;

namespace CMS.Infrastructure.Security;

public sealed class HtmlContentSanitizer : IHtmlContentSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new HtmlSanitizer();

        _sanitizer.AllowedTags.UnionWith(
        [
            "h1", "h2", "h3", "h4",
            "p", "br", "hr",
            "strong", "b", "em", "i", "u", "s", "sub", "sup", "mark",
            "ul", "ol", "li",
            "blockquote", "pre", "code",
            "a", "img", "figure", "figcaption",
            "span", "div",
            "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "colgroup", "col",
            "video", "source"
        ]);

        _sanitizer.AllowedAttributes.UnionWith(
        [
            "href", "src", "alt", "title", "class", "id",
            "target", "rel", "width", "height",
            "controls", "playsinline", "poster", "type",
            "colspan", "rowspan", "scope", "span",
            "dir", "lang", "loading", "style"
        ]);

        _sanitizer.AllowedCssProperties.UnionWith(
        [
            "color", "background", "background-color", "background-image",
            "text-align", "font-size", "font-weight", "font-style", "font-family",
            "line-height", "letter-spacing", "text-decoration",
            "width", "height", "max-width", "min-width", "min-height", "max-height",
            "border", "border-top", "border-right", "border-bottom", "border-left",
            "border-inline-start", "border-inline-end", "border-block-start", "border-block-end",
            "border-color", "border-style", "border-width", "border-radius", "border-collapse",
            "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
            "padding-inline", "padding-inline-start", "padding-inline-end",
            "padding-block", "padding-block-start", "padding-block-end",
            "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
            "margin-inline", "margin-inline-start", "margin-inline-end",
            "margin-block", "margin-block-start", "margin-block-end",
            "display", "flex", "flex-wrap", "flex-direction", "flex-grow", "flex-shrink", "flex-basis",
            "align-items", "justify-content", "gap", "row-gap", "column-gap",
            "grid-template-columns", "place-items", "object-fit", "aspect-ratio",
            "overflow", "position", "inset", "inset-block-start", "inset-inline-end",
            "top", "right", "bottom", "left", "z-index", "pointer-events",
            "box-shadow", "opacity", "white-space", "unicode-bidi", "letter-spacing",
            "backdrop-filter", "-webkit-backdrop-filter",
            "list-style-type", "list-style", "direction"
        ]);

        _sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto", "tel"]);
        _sanitizer.AllowDataAttributes = false;
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}
