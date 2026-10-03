using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Voices.Domain.Entities;

public class VoiceItem : BaseEntity
{
    private VoiceItem()
    {
    }

    public string CustomerName { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string AudioUrl { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public static VoiceItem Create(
        string customerName,
        string? subtitle,
        string? description,
        string audioUrl,
        int sortOrder,
        bool isPublished)
    {
        var item = new VoiceItem();
        item.ApplyContent(customerName, subtitle, description, audioUrl, sortOrder);
        if (isPublished)
            item.Publish();
        return item;
    }

    public void Update(
        string customerName,
        string? subtitle,
        string? description,
        string audioUrl,
        int sortOrder,
        bool isPublished)
    {
        ApplyContent(customerName, subtitle, description, audioUrl, sortOrder);
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

    private void ApplyContent(
        string customerName,
        string? subtitle,
        string? description,
        string audioUrl,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new DomainException("نام کاربر الزامی است.");
        if (customerName.Trim().Length > 200)
            throw new DomainException("نام کاربر خیلی طولانی است.");
        if (subtitle is { Length: > 300 })
            throw new DomainException("زیرعنوان خیلی طولانی است.");
        if (description is { Length: > 2000 })
            throw new DomainException("توضیحات خیلی طولانی است.");
        if (string.IsNullOrWhiteSpace(audioUrl))
            throw new DomainException("آدرس فایل صوتی الزامی است.");
        if (audioUrl.Trim().Length > 1000)
            throw new DomainException("آدرس فایل صوتی خیلی طولانی است.");

        CustomerName = customerName.Trim();
        Subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        AudioUrl = audioUrl.Trim();
        SortOrder = sortOrder;
    }
}
