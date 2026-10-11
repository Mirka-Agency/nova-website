using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace CMS.Modules.Seo.Web.TagHelpers;

internal static class TagHelperAttributeValue
{
    public static string? Read(TagHelperOutput output, string name)
    {
        if (!output.Attributes.TryGetAttribute(name, out var attr) || attr.Value is null)
            return null;

        return Normalize(attr.Value);
    }

    public static string? Normalize(object? value)
    {
        if (value is null)
            return null;

        var text = value switch
        {
            string s => s,
            IHtmlContent html => WriteHtml(html),
            _ => value.ToString()
        };

        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private static string WriteHtml(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
