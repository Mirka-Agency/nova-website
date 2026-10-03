namespace CMS.Modules.Shop.Application.Reports;

public sealed record SalesSummaryDto(
    decimal TotalSales,
    int OrderCount,
    decimal TodaySales,
    decimal MonthSales);

public sealed record BestSellerDto(Guid ProductId, string Title, int QuantitySold, decimal Revenue);
public sealed record TopCustomerDto(string CustomerEmail, string CustomerName, int OrderCount, decimal TotalSpent);
public sealed record LowStockReportItemDto(Guid ProductId, string Title, Guid? VariationId, string? Sku, int Quantity);

public sealed record CommerceReportDto(
    SalesSummaryDto Summary,
    IReadOnlyList<BestSellerDto> BestSellers,
    IReadOnlyList<TopCustomerDto> TopCustomers,
    IReadOnlyList<LowStockReportItemDto> LowStock);
