namespace CMS.Application.Security;

/// <summary>Sanitizes HTML from rich-text editors before persistence.</summary>
public interface IHtmlContentSanitizer
{
    string Sanitize(string? html);
}
