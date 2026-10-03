using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Reports;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class CommerceReportService : ICommerceReportService
{
    private readonly ShopDbContext _db;

    public CommerceReportService(ShopDbContext db)
    {
        _db = db;
    }

    public async Task<CommerceReportDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var todayStart = utcNow.Date;
        var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var salesQuery = _db.Orders.AsNoTracking()
            .Where(o => o.PaymentStatus == PaymentStatus.Paid || o.Status == OrderStatus.Completed);

        var summary = new SalesSummaryDto(
            await salesQuery.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m,
            await salesQuery.CountAsync(cancellationToken),
            await salesQuery.Where(o => o.CreatedAtUtc >= todayStart)
                .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m,
            await salesQuery.Where(o => o.CreatedAtUtc >= monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m);

        var bestSellers = await _db.OrderLines
            .AsNoTracking()
            .Where(l => l.Order.PaymentStatus == PaymentStatus.Paid || l.Order.Status == OrderStatus.Completed)
            .GroupBy(l => new { l.ProductId, l.ProductTitle })
            .Select(g => new BestSellerDto(
                g.Key.ProductId,
                g.Key.ProductTitle,
                g.Sum(l => l.Quantity),
                g.Sum(l => l.UnitPrice * l.Quantity)))
            .OrderByDescending(x => x.QuantitySold)
            .Take(10)
            .ToListAsync(cancellationToken);

        var topCustomers = await salesQuery
            .GroupBy(o => new { o.CustomerEmail, o.CustomerName })
            .Select(g => new TopCustomerDto(
                g.Key.CustomerEmail,
                g.Key.CustomerName,
                g.Count(),
                g.Sum(o => o.TotalAmount)))
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .ToListAsync(cancellationToken);

        var lowStockProducts = await _db.Products
            .AsNoTracking()
            .Where(p => !p.UnlimitedStock
                && p.StockQuantity != null
                && p.StockQuantity <= p.LowStockThreshold)
            .OrderBy(p => p.StockQuantity)
            .Take(50)
            .Select(p => new LowStockReportItemDto(
                p.Id,
                p.Title,
                null,
                p.Sku,
                p.StockQuantity ?? 0))
            .ToListAsync(cancellationToken);

        var lowStockVariations = await _db.Variations
            .AsNoTracking()
            .Where(v => !v.UnlimitedStock
                && v.StockQuantity != null
                && v.StockQuantity <= (v.Product.LowStockThreshold > 0 ? v.Product.LowStockThreshold : 5))
            .OrderBy(v => v.StockQuantity)
            .Take(50)
            .Select(v => new LowStockReportItemDto(
                v.ProductId,
                v.Product.Title,
                v.Id,
                v.Sku,
                v.StockQuantity ?? 0))
            .ToListAsync(cancellationToken);

        var lowStock = lowStockProducts
            .Concat(lowStockVariations)
            .OrderBy(x => x.Quantity)
            .Take(50)
            .ToList();

        return new CommerceReportDto(summary, bestSellers, topCustomers, lowStock);
    }
}
