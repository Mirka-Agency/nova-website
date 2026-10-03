using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Honors.Domain.Entities;

public class HonorItem : BaseEntity
{
    private HonorItem()
    {
    }

    public string Title { get; private set; } = string.Empty;
    public string ImageUrl { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public static HonorItem Create(
        string title,
        string imageUrl,
        string? altText,
        int sortOrder,
        bool isPublished)
    {
        var item = new HonorItem();
        item.ApplyContent(title, imageUrl, altText, sortOrder);
        if (isPublished)
            item.Publish();
        return item;
    }

    public void Update(
        string title,
        string imageUrl,
        string? altText,
        int sortOrder,
        bool isPublished)
    {
        ApplyContent(title, imageUrl, altText, sortOrder);
        if (isPublished)
            Publish();
        else
            Unpublish();
        Touch();
    }

    public void Publish(DateTime? publishedAtUtc = null)
    {
        IsPublished = true;
        if (publishedAtUtc.HasValue)
            PublishedAtUtc = DateTime.SpecifyKind(publishedAtUtc.Value, DateTimeKind.Utc);
        else
            PublishedAtUtc ??= DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        IsPublished = false;
        Touch();
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
        Touch();
    }

    private void ApplyContent(string title, string imageUrl, string? altText, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("عنوان افتخار الزامی است.");
        if (title.Trim().Length > 200)
            throw new DomainException("عنوان افتخار خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new DomainException("آدرس تصویر الزامی است.");
        if (imageUrl.Trim().Length > 1000)
            throw new DomainException("آدرس تصویر خیلی طولانی است.");
        if (altText is { Length: > 300 })
            throw new DomainException("متن جایگزین خیلی طولانی است.");

        Title = title.Trim();
        ImageUrl = imageUrl.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        SortOrder = sortOrder;
    }
}
