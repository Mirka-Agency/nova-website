using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductImage : BaseEntity
{
    private ProductImage()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsMain { get; private set; }

    public static ProductImage Create(Guid productId, string url, string? altText, int sortOrder, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("آدرس تصویر الزامی است.");

        return new ProductImage
        {
            ProductId = productId,
            Url = url.Trim(),
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            SortOrder = Math.Max(0, sortOrder),
            IsMain = isMain
        };
    }

    public void Update(string url, string? altText, int sortOrder, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("آدرس تصویر الزامی است.");

        Url = url.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        SortOrder = Math.Max(0, sortOrder);
        IsMain = isMain;
        Touch();
    }
}
