using CMS.Domain.Common;
using CMS.Domain.Exceptions;

namespace CMS.Modules.Shop.Domain.Entities;

public class ProductReview : BaseEntity
{
    private ProductReview()
    {
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string? UserId { get; private set; }
    public string AuthorName { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public bool IsApproved { get; private set; }
    public string? AdminResponse { get; private set; }

    public static ProductReview Create(
        Guid productId,
        string? userId,
        string authorName,
        int rating,
        string comment)
    {
        if (string.IsNullOrWhiteSpace(authorName))
            throw new DomainException("نام نویسنده الزامی است.");
        if (rating is < 1 or > 5)
            throw new DomainException("امتیاز باید بین ۱ تا ۵ باشد.");

        return new ProductReview
        {
            ProductId = productId,
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            AuthorName = authorName.Trim(),
            Rating = rating,
            Comment = comment?.Trim() ?? string.Empty,
            IsApproved = false
        };
    }

    public void Approve()
    {
        IsApproved = true;
        Touch();
    }

    public void Reject()
    {
        IsApproved = false;
        Touch();
    }

    public void SetAdminResponse(string? response)
    {
        AdminResponse = string.IsNullOrWhiteSpace(response) ? null : response.Trim();
        Touch();
    }
}
