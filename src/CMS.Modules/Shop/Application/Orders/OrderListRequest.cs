using CMS.Application.Common.Paging;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Orders;

public sealed class OrderListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = PagedRequest.DefaultPageSize;
    public string? Search { get; init; }
    public IReadOnlyCollection<OrderStatus>? Statuses { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public bool? IsWholesale { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    /// <summary>newest | oldest | total | total_desc | status | customer</summary>
    public string? Sort { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize =>
        PageSize < 1 ? PagedRequest.DefaultPageSize : Math.Min(PageSize, PagedRequest.MaxPageSize);

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
