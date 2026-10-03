using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Promotions;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class CouponService : ICouponService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SaveCouponCommand> _validator;

    public CouponService(ShopDbContext db, IValidator<SaveCouponCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new PagedRequest { Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<CouponDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = _db.Coupons.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(c => Map(c))
            .ToListAsync(cancellationToken);

        return new PagedResult<CouponDto>(
            items,
            total,
            request.NormalizedPage,
            request.NormalizedPageSize);
    }

    public async Task<Guid> CreateAsync(SaveCouponCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var code = command.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == code, cancellationToken))
            throw new DomainException("کد کوپن تکراری است.");

        var coupon = Coupon.Create(
            code,
            command.Name,
            command.DiscountType,
            command.Amount,
            command.ProductId,
            command.CategoryId,
            command.MinQuantity,
            command.MinOrderAmount,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.MaxRedemptions,
            command.AllowedUserId);

        if (!command.IsActive)
            coupon.Update(code, command.Name, command.DiscountType, command.Amount, command.ProductId,
                command.CategoryId, command.MinQuantity, command.MinOrderAmount, command.StartsAtUtc,
                command.EndsAtUtc, command.MaxRedemptions, false, command.AllowedUserId);

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(cancellationToken);
        return coupon.Id;
    }

    public async Task UpdateAsync(Guid id, SaveCouponCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), id);

        var code = command.Code.Trim().ToUpperInvariant();
        if (await _db.Coupons.AnyAsync(c => c.Code == code && c.Id != id, cancellationToken))
            throw new DomainException("کد کوپن تکراری است.");

        coupon.Update(
            code,
            command.Name,
            command.DiscountType,
            command.Amount,
            command.ProductId,
            command.CategoryId,
            command.MinQuantity,
            command.MinOrderAmount,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.MaxRedemptions,
            command.IsActive,
            command.AllowedUserId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Coupon), id);
        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static decimal CalculateDiscount(
        Coupon coupon,
        IReadOnlyList<CouponLineContext> lines,
        decimal subtotal)
    {
        var utcNow = DateTime.UtcNow;
        if (!coupon.IsEffective(utcNow))
            return 0;

        if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
            return 0;

        var totalQty = lines.Sum(l => l.Quantity);
        if (coupon.MinQuantity.HasValue && totalQty < coupon.MinQuantity.Value)
            return 0;

        return coupon.DiscountType switch
        {
            DiscountType.Percentage => Math.Round(subtotal * coupon.Amount / 100m, 2),
            DiscountType.FixedAmount => Math.Min(coupon.Amount, subtotal),
            DiscountType.Product => CalculateScopedDiscount(coupon, lines, subtotal, l => l.ProductId == coupon.ProductId),
            DiscountType.Category => CalculateScopedDiscount(coupon, lines, subtotal, l => l.CategoryId == coupon.CategoryId),
            DiscountType.Quantity => coupon.Amount * totalQty,
            _ => 0
        };
    }

    private static decimal CalculateScopedDiscount(
        Coupon coupon,
        IReadOnlyList<CouponLineContext> lines,
        decimal subtotal,
        Func<CouponLineContext, bool> predicate)
    {
        var scoped = lines.Where(predicate).ToList();
        if (scoped.Count == 0)
            return 0;

        var scopedSubtotal = scoped.Sum(l => l.LineTotal);
        return coupon.DiscountType switch
        {
            DiscountType.Product or DiscountType.Category when coupon.Amount <= 100 && coupon.Amount > 0 && coupon.Amount == Math.Floor(coupon.Amount)
                => Math.Round(scopedSubtotal * coupon.Amount / 100m, 2),
            _ => Math.Min(coupon.Amount, scopedSubtotal)
        };
    }

    private async Task ValidateAsync(SaveCouponCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static CouponDto Map(Coupon c) =>
        new(c.Id, c.Code, c.Name, c.DiscountType, c.Amount, c.ProductId, c.CategoryId,
            c.MinQuantity, c.MinOrderAmount, c.StartsAtUtc, c.EndsAtUtc,
            c.MaxRedemptions, c.RedemptionCount, c.IsActive, c.AllowedUserId);
}

internal sealed record CouponLineContext(
    Guid ProductId,
    Guid? CategoryId,
    int Quantity,
    decimal LineTotal);
