using System.Text.Json;
using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Media.Domain.ValueObjects;

namespace CMS.Modules.Media.Domain.Entities;

public class MediaAsset : BaseEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private MediaAsset()
    {
    }

    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? AltText { get; private set; }
    public string? Caption { get; private set; }
    public string? Description { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public string? ThumbnailObjectKey { get; private set; }
    public string? OriginalFileName { get; private set; }
    public string? OriginalContentType { get; private set; }
    public long? OriginalSizeBytes { get; private set; }
    public string? OriginalObjectKey { get; private set; }
    public string? VariantsJson { get; private set; }

    public bool HasSeparateOriginal =>
        !string.IsNullOrWhiteSpace(OriginalObjectKey)
        && !string.Equals(OriginalObjectKey, ObjectKey, StringComparison.Ordinal);

    public static MediaAsset Create(
        MediaFileRef display,
        MediaFileRef original,
        string? title = null,
        string? altText = null,
        string? caption = null,
        string? description = null,
        MediaFileRef? thumbnail = null,
        IReadOnlyList<MediaImageVariant>? variants = null)
    {
        ValidateFile(display, "نمایش");
        ValidateFile(original, "اصلی");

        var asset = new MediaAsset();
        asset.ApplyDisplay(display, thumbnail, variants);
        asset.ApplyOriginal(original);
        asset.ApplyMetadata(title ?? Path.GetFileNameWithoutExtension(original.FileName), altText, caption, description);
        return asset;
    }

    public void UpdateMetadata(string? title, string? altText, string? caption, string? description)
    {
        ApplyMetadata(title, altText, caption, description);
        Touch();
    }

    /// <summary>
    /// Keeps the original file and swaps the optimized/display derivatives.
    /// Returns storage keys that are no longer referenced (never includes the original).
    /// </summary>
    public IReadOnlyList<string> ReplaceDisplay(
        MediaFileRef display,
        MediaFileRef? thumbnail = null,
        IReadOnlyList<MediaImageVariant>? variants = null)
    {
        ValidateFile(display, "نمایش");
        EnsureOriginalCaptured();
        var staleKeys = GetDerivativeKeys();
        ApplyDisplay(display, thumbnail, variants);
        Touch();
        return staleKeys;
    }

    public IReadOnlyList<string> RestoreOriginal()
    {
        EnsureOriginalCaptured();
        if (!HasSeparateOriginal)
            throw new DomainException("نسخهٔ اصلی همین فایل در حال نمایش است.");

        var staleKeys = GetDerivativeKeys();
        ApplyDisplay(new MediaFileRef(
            OriginalFileName!,
            OriginalContentType!,
            OriginalSizeBytes!.Value,
            OriginalObjectKey!), thumbnail: null, variants: null);
        Touch();
        return staleKeys;
    }

    public IReadOnlyList<string> GetAllStorageKeys()
    {
        var keys = new List<string> { ObjectKey };
        if (!string.IsNullOrWhiteSpace(OriginalObjectKey))
            keys.Add(OriginalObjectKey);
        keys.AddRange(GetDerivativeKeys());
        return keys.Distinct(StringComparer.Ordinal).ToList();
    }

    public IReadOnlyList<MediaImageVariant> GetVariants()
    {
        if (string.IsNullOrWhiteSpace(VariantsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<MediaImageVariant>>(VariantsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void EnsureOriginalCaptured()
    {
        if (!string.IsNullOrWhiteSpace(OriginalObjectKey))
            return;

        OriginalFileName = FileName;
        OriginalContentType = ContentType;
        OriginalSizeBytes = SizeBytes;
        OriginalObjectKey = ObjectKey;
    }

    private void ApplyOriginal(MediaFileRef original)
    {
        OriginalFileName = Path.GetFileName(original.FileName.Trim());
        OriginalContentType = original.ContentType.Trim();
        OriginalSizeBytes = original.SizeBytes;
        OriginalObjectKey = original.ObjectKey.Trim();
    }

    private void ApplyDisplay(
        MediaFileRef display,
        MediaFileRef? thumbnail,
        IReadOnlyList<MediaImageVariant>? variants)
    {
        FileName = Path.GetFileName(display.FileName.Trim());
        ContentType = display.ContentType.Trim();
        SizeBytes = display.SizeBytes;
        ObjectKey = display.ObjectKey.Trim();
        Width = display.Width;
        Height = display.Height;
        ThumbnailObjectKey = string.IsNullOrWhiteSpace(thumbnail?.ObjectKey) ? null : thumbnail.ObjectKey.Trim();
        VariantsJson = variants is { Count: > 0 }
            ? JsonSerializer.Serialize(variants, JsonOptions)
            : null;
    }

    private IReadOnlyList<string> GetDerivativeKeys()
    {
        var keys = new List<string>();
        if (HasSeparateOriginal)
            keys.Add(ObjectKey);
        if (!string.IsNullOrWhiteSpace(ThumbnailObjectKey)
            && !string.Equals(ThumbnailObjectKey, OriginalObjectKey, StringComparison.Ordinal)
            && !string.Equals(ThumbnailObjectKey, ObjectKey, StringComparison.Ordinal))
        {
            keys.Add(ThumbnailObjectKey);
        }

        foreach (var variant in GetVariants())
        {
            if (string.IsNullOrWhiteSpace(variant.ObjectKey))
                continue;
            if (string.Equals(variant.ObjectKey, OriginalObjectKey, StringComparison.Ordinal)
                || string.Equals(variant.ObjectKey, ObjectKey, StringComparison.Ordinal)
                || string.Equals(variant.ObjectKey, ThumbnailObjectKey, StringComparison.Ordinal))
                continue;
            keys.Add(variant.ObjectKey);
        }

        return keys.Distinct(StringComparer.Ordinal).ToList();
    }

    private void ApplyMetadata(string? title, string? altText, string? caption, string? description)
    {
        Title = NormalizeOptional(title, 300);
        AltText = NormalizeOptional(altText, 500);
        Caption = NormalizeOptional(caption, 1000);
        Description = NormalizeOptional(description, 4000);
    }

    private static void ValidateFile(MediaFileRef file, string label)
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
            throw new DomainException($"نام فایل {label} الزامی است.");
        if (string.IsNullOrWhiteSpace(file.ContentType))
            throw new DomainException($"نوع محتوای {label} الزامی است.");
        if (file.SizeBytes <= 0)
            throw new DomainException($"حجم فایل {label} باید مثبت باشد.");
        if (string.IsNullOrWhiteSpace(file.ObjectKey))
            throw new DomainException($"کلید شیء {label} الزامی است.");
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"مقدار بیش از {maxLength} کاراکتر است.");
        return trimmed;
    }
}
