using CMS.Application.Common.Paging;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Products;

public sealed class ProductListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public ProductStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public ProductType? ProductType { get; init; }
    public bool? IsAvailable { get; init; }
    public bool? IsPurchasable { get; init; }

    /// <summary>in | out | low | unlimited</summary>
    public string? Stock { get; init; }

    /// <summary>newest | title | title_desc | price | price_desc | stock | status</summary>
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
