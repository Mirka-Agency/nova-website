using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class PricingEngine : IPricingEngine
{
    private readonly ShopDbContext _db;

    public PricingEngine(ShopDbContext db)
    {
        _db = db;
    }

    public async Task<Guid?> ResolveCustomerGroupIdAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _db.CustomerGroupMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => (Guid?)m.CustomerGroupId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PriceQuoteResult> QuoteAsync(PriceQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var results = await QuoteManyAsync([request], cancellationToken);
        if (!results.TryGetValue((request.ProductId, request.VariationId), out var result))
            throw new NotFoundException(nameof(Product), request.ProductId);

        return result;
    }

    public async Task<IReadOnlyDictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult>> QuoteManyAsync(
        IReadOnlyList<PriceQuoteRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
            return new Dictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult>();

        var productIds = requests.Select(r => r.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variations)
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var rules = await _db.PriceRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var utcNow = DateTime.UtcNow;
        var results = new Dictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult>(requests.Count);

        foreach (var request in requests)
        {
            if (!products.TryGetValue(request.ProductId, out var product))
                throw new NotFoundException(nameof(Product), request.ProductId);

            ProductVariation? variation = null;
            if (request.VariationId.HasValue)
            {
                variation = product.Variations.FirstOrDefault(v => v.Id == request.VariationId.Value)
                    ?? throw new NotFoundException(nameof(ProductVariation), request.VariationId.Value);
            }

            results[(request.ProductId, request.VariationId)] =
                QuoteInMemory(product, variation, request, rules, utcNow);
        }

        return results;
    }

    private static PriceQuoteResult QuoteInMemory(
        Product product,
        ProductVariation? variation,
        PriceQuoteRequest request,
        IReadOnlyList<PriceRule> activeRules,
        DateTime utcNow)
    {
        var regularPrice = variation?.Price ?? product.Price;
        var unitPrice = variation?.EffectivePrice ?? product.GetEffectiveRetailPrice(utcNow);
        var customerGroupId = request.CustomerGroupId;

        var matching = activeRules
            .Where(r =>
                (!r.ProductId.HasValue || r.ProductId == request.ProductId)
                && (!r.VariationId.HasValue || r.VariationId == request.VariationId)
                && RuleMatches(r, request, customerGroupId, utcNow))
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => RuleTypeRank(r.RuleType))
            .ToList();

        Guid? appliedRuleId = null;
        string? appliedRuleName = null;

        foreach (var rule in matching)
        {
            if (rule.FixedPrice.HasValue)
                unitPrice = rule.FixedPrice.Value;
            else if (rule.DiscountPercent.HasValue)
                unitPrice = Math.Round(unitPrice * (1 - rule.DiscountPercent.Value / 100m), 2);

            appliedRuleId = rule.Id;
            appliedRuleName = rule.Name;
        }

        if (unitPrice < 0)
            unitPrice = 0;

        return new PriceQuoteResult(
            regularPrice,
            unitPrice,
            unitPrice * request.Quantity,
            appliedRuleId,
            appliedRuleName);
    }

    private static bool RuleMatches(PriceRule rule, PriceQuoteRequest request, Guid? customerGroupId, DateTime utcNow)
    {
        if (!rule.IsEffective(utcNow))
            return false;

        return rule.RuleType switch
        {
            PriceRuleType.CustomerGroup =>
                rule.CustomerGroupId.HasValue && rule.CustomerGroupId == customerGroupId,
            PriceRuleType.QuantityTier =>
                (!rule.MinQuantity.HasValue || request.Quantity >= rule.MinQuantity)
                && (!rule.MaxQuantity.HasValue || request.Quantity <= rule.MaxQuantity),
            PriceRuleType.RetailSale => true,
            _ => false
        };
    }

    private static int RuleTypeRank(PriceRuleType ruleType) =>
        ruleType switch
        {
            PriceRuleType.CustomerGroup => 3,
            PriceRuleType.QuantityTier => 2,
            PriceRuleType.RetailSale => 1,
            _ => 0
        };
}
