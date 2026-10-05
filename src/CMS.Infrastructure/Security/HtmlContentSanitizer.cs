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
            "dir", "lang"
        ]);

        _sanitizer.AllowedCssProperties.UnionWith(
        [
            "color", "background-color",
            "text-align", "font-size", "font-weight", "font-style",
            "text-decoration", "width", "height", "max-width",
            "border", "border-collapse", "padding", "margin",
            "list-style-type", "list-style", "direction"
        ]);

        _sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        _sanitizer.AllowDataAttributes = false;
    }

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}
