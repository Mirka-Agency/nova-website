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
        ToUrl = toUrl.Trim();
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

        if (statusCode is not (301 or 302))
            throw new DomainException("کد وضعیت باید ۳۰۱ یا ۳۰۲ باشد.");

        if (!string.IsNullOrWhiteSpace(note) && note.Trim().Length > 500)
            throw new DomainException("یادداشت خیلی طولانی است.");
    }

    public static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var trimmed = path.Trim();
        if (!trimmed.StartsWith('/'))
            trimmed = "/" + trimmed;

        if (trimmed.Length > 1)
            trimmed = trimmed.TrimEnd('/');

        return trimmed;
    }
}
