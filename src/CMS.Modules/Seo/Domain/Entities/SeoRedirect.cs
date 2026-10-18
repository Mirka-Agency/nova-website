using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Seo.Domain.Entities;

public class SeoRedirect : BaseEntity
{
    private SeoRedirect()
    {
    }

    public string FromPath { get; private set; } = string.Empty;
    public string ToUrl { get; private set; } = string.Empty;
    public int StatusCode { get; private set; } = 301;
    public bool IsActive { get; private set; } = true;
    public string? Note { get; private set; }

    public static SeoRedirect Create(string fromPath, string toUrl, int statusCode, bool isActive, string? note)
    {
        var item = new SeoRedirect();
        item.Apply(fromPath, toUrl, statusCode, isActive, note);
        return item;
    }

    public void Update(string fromPath, string toUrl, int statusCode, bool isActive, string? note)
    {
        Apply(fromPath, toUrl, statusCode, isActive, note);
        Touch();
    }

    public void SetActive(bool isActive)
    {
        if (IsActive == isActive)
            return;
        IsActive = isActive;
        Touch();
    }

    private void Apply(string fromPath, string toUrl, int statusCode, bool isActive, string? note)
    {
        Validate(fromPath, toUrl, statusCode, note);

        FromPath = NormalizePath(fromPath);
        ToUrl = NormalizeTarget(toUrl);
        StatusCode = statusCode;
        IsActive = isActive;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static void Validate(string fromPath, string toUrl, int statusCode, string? note)
    {
        if (string.IsNullOrWhiteSpace(fromPath))
            throw new DomainException("مسیر مبدأ الزامی است.");

        var normalized = NormalizePath(fromPath);
        if (!normalized.StartsWith('/'))
            throw new DomainException("مسیر مبدأ باید با / شروع شود.");

        if (normalized.Length > 500)
            throw new DomainException("مسیر مبدأ خیلی طولانی است.");

        if (string.IsNullOrWhiteSpace(toUrl))
            throw new DomainException("آدرس مقصد الزامی است.");

        if (toUrl.Trim().Length > 2000)
            throw new DomainException("آدرس مقصد خیلی طولانی است.");

        if (statusCode is not (200 or 301 or 302))
            throw new DomainException("کد وضعیت باید ۲۰۰، ۳۰۱ یا ۳۰۲ باشد.");

        if (statusCode == 200)
        {
            var trimmed = toUrl.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
                throw new DomainException("برای بازنویسی ۲۰۰ مقصد باید مسیر داخلی باشد (مثلاً /page)، نه آدرس کامل.");

            var pathPart = trimmed.Split('?', 2)[0];
            var targetPath = NormalizePath(pathPart);
            if (!targetPath.StartsWith('/'))
                throw new DomainException("برای بازنویسی ۲۰۰ مقصد باید با / شروع شود.");

            if (string.Equals(normalized, targetPath, StringComparison.OrdinalIgnoreCase))
                throw new DomainException("مسیر مبدأ و مقصد بازنویسی نمی‌توانند یکسان باشند.");
        }

        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 500)
            throw new DomainException("یادداشت خیلی طولانی است.");
    }

    private static string NormalizeTarget(string toUrl)
    {
        var trimmed = toUrl.Trim();

        // Protocol-relative or absolute http(s) destinations stay as entered.
        if (trimmed.StartsWith("//", StringComparison.Ordinal))
            return trimmed;

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return trimmed;
        }

        // Relative internal targets use the public form (trailing slash) so the next
        // middleware hop is not needed for 301/302 or for 200 rewrites.
        var parts = trimmed.Split('?', 2);
        var publicPath = ToPublicPath(parts[0]);
        return parts.Length > 1 ? $"{publicPath}?{parts[1]}" : publicPath;
    }

    /// <summary>
    /// Lookup key form: leading slash, collapsed duplicate slashes, no trailing slash (except root).
    /// </summary>
    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/'))
            trimmed = "/" + trimmed;

        trimmed = CollapseDuplicateSlashes(trimmed);

        if (trimmed.Length > 1)
            trimmed = trimmed.TrimEnd('/');

        return trimmed;
    }

    /// <summary>
    /// Public relative path form used for redirect Location / rewrite Path.
    /// </summary>
    public static string ToPublicPath(string? path)
    {
        var key = NormalizePath(path);
        if (key.Length <= 1)
            return "/";

        return key.ToLowerInvariant() + "/";
    }

    private static string CollapseDuplicateSlashes(string path)
    {
        if (!path.Contains("//", StringComparison.Ordinal))
            return path;

        var buffer = new char[path.Length];
        var length = 0;
        var previousSlash = false;
        foreach (var c in path)
        {
            if (c == '/')
            {
                if (previousSlash)
                    continue;
                previousSlash = true;
            }
            else
            {
                previousSlash = false;
            }

            buffer[length++] = c;
        }

        return length == path.Length ? path : new string(buffer, 0, length);
    }
}
