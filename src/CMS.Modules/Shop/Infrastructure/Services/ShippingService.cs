using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ShippingService : IShippingService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SaveShippingMethodCommand> _validator;

    public ShippingService(ShopDbContext db, IValidator<SaveShippingMethodCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<IReadOnlyList<ShippingMethodDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var methods = await _db.ShippingMethods
            .AsNoTracking()
            .Include(m => m.Rates)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return methods.Select(Map).ToList();
    }

    public async Task<Guid> CreateAsync(SaveShippingMethodCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var method = ShippingMethod.Create(
            command.Name,
            command.Description,
            command.CalculationType,
            command.FixedCost,
            command.FreeShippingMinAmount,
            command.EstimatedDeliveryText,
            command.SortOrder);

        if (!command.IsActive)
            method.Update(command.Name, command.Description, command.CalculationType, command.FixedCost,
                command.FreeShippingMinAmount, command.EstimatedDeliveryText, false, command.SortOrder);

        ApplyRates(method, command.Rates);
        _db.ShippingMethods.Add(method);
        await _db.SaveChangesAsync(cancellationToken);
        return method.Id;
    }

    public async Task UpdateAsync(Guid id, SaveShippingMethodCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var method = await _db.ShippingMethods
            .Include(m => m.Rates)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShippingMethod), id);

        method.Update(
            command.Name,
            command.Description,
            command.CalculationType,
            command.FixedCost,
            command.FreeShippingMinAmount,
            command.EstimatedDeliveryText,
            command.IsActive,
            command.SortOrder);

        _db.ShippingRates.RemoveRange(method.Rates);
        method.ClearRates();
        ApplyRates(method, command.Rates);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var method = await _db.ShippingMethods.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ShippingMethod), id);
        _db.ShippingMethods.Remove(method);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShippingQuoteResult?> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var method = await _db.ShippingMethods
            .AsNoTracking()
            .Include(m => m.Rates)
            .FirstOrDefaultAsync(m => m.Id == request.MethodId && m.IsActive, cancellationToken);

        if (method is null)
            return null;

        if (method.FreeShippingMinAmount.HasValue && request.Subtotal >= method.FreeShippingMinAmount.Value)
            return new ShippingQuoteResult(method.Id, method.Name, 0, method.EstimatedDeliveryText);

        var cost = method.CalculationType switch
        {
            ShippingCalculationType.Fixed => method.FixedCost,
            ShippingCalculationType.WeightBased => ResolveWeightCost(method, request.TotalWeight),
            ShippingCalculationType.CityBased => ResolveCityCost(method, request.Province, request.City),
            _ => method.FixedCost
        };

        return new ShippingQuoteResult(method.Id, method.Name, Math.Max(0, cost), method.EstimatedDeliveryText);
    }

    private static decimal ResolveWeightCost(ShippingMethod method, decimal totalWeight)
    {
        var rate = method.Rates
            .Where(r => (!r.MinWeight.HasValue || totalWeight >= r.MinWeight)
                && (!r.MaxWeight.HasValue || totalWeight <= r.MaxWeight))
            .OrderBy(r => r.Cost)
            .FirstOrDefault();

        return rate?.Cost ?? method.FixedCost;
    }

    private static decimal ResolveCityCost(ShippingMethod method, string? province, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return method.FixedCost;

        var cityName = city.Trim();
        var provinceName = province?.Trim();

        var rate = method.Rates.FirstOrDefault(r =>
            r.City != null
            && string.Equals(r.City, cityName, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(r.Province)
                || string.IsNullOrWhiteSpace(provinceName)
                || string.Equals(r.Province, provinceName, StringComparison.OrdinalIgnoreCase)));

        return rate?.Cost ?? method.FixedCost;
    }

    private static void ApplyRates(ShippingMethod method, IReadOnlyList<SaveShippingRateCommand>? rates)
    {
        if (rates is null)
            return;

        foreach (var rate in rates)
            method.AddRate(rate.Province, rate.City, rate.MinWeight, rate.MaxWeight, rate.Cost);
    }

    private async Task ValidateAsync(SaveShippingMethodCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static ShippingMethodDto Map(ShippingMethod m) =>
        new(
            m.Id,
            m.Name,
            m.Description,
            m.CalculationType,
            m.FixedCost,
            m.FreeShippingMinAmount,
            m.EstimatedDeliveryText,
            m.IsActive,
            m.SortOrder,
            m.Rates.Select(r => new ShippingRateDto(r.Id, r.Province, r.City, r.MinWeight, r.MaxWeight, r.Cost)).ToList());
}
