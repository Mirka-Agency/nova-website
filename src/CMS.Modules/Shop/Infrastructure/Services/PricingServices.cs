using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class CustomerGroupService : ICustomerGroupService
{
    private static readonly (string Name, string Code, bool IsWholesale, int SortOrder)[] DefaultGroups =
    [
        ("Normal", "NORMAL", false, 0),
        ("Wholesale", "WHOLESALE", true, 1),
        ("Partner", "PARTNER", true, 2),
        ("Distributor", "DISTRIBUTOR", true, 3)
    ];

    private readonly ShopDbContext _db;
    private readonly IValidator<SaveCustomerGroupCommand> _validator;

    public CustomerGroupService(ShopDbContext db, IValidator<SaveCustomerGroupCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<IReadOnlyList<CustomerGroupDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.CustomerGroups
            .AsNoTracking()
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Name)
            .Select(g => Map(g))
            .ToListAsync(cancellationToken);

    public async Task<CustomerGroupDto?> GetUserGroupAsync(string userId, CancellationToken cancellationToken = default)
    {
        var group = await _db.CustomerGroupMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.CustomerGroup)
            .FirstOrDefaultAsync(cancellationToken);

        return group is null ? null : Map(group);
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (name, code, isWholesale, sortOrder) in DefaultGroups)
        {
            if (await _db.CustomerGroups.AnyAsync(g => g.Code == code, cancellationToken))
                continue;

            _db.CustomerGroups.Add(CustomerGroup.Create(name, code, null, isWholesale, sortOrder));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveCustomerGroupCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        if (await _db.CustomerGroups.AnyAsync(g => g.Code == command.Code.Trim().ToUpperInvariant(), cancellationToken))
            throw new DomainException("کد گروه مشتری تکراری است.");

        var group = CustomerGroup.Create(
            command.Name,
            command.Code,
            command.Description,
            command.IsWholesale,
            command.SortOrder,
            command.MinimumOrderAmount,
            command.MinimumOrderQuantity);

        if (!command.IsActive)
            group.Update(command.Name, command.Code, command.Description, command.IsWholesale, command.SortOrder,
                false, command.MinimumOrderAmount, command.MinimumOrderQuantity);

        _db.CustomerGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return group.Id;
    }

    public async Task UpdateAsync(Guid id, SaveCustomerGroupCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var group = await _db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerGroup), id);

        var code = command.Code.Trim().ToUpperInvariant();
        if (await _db.CustomerGroups.AnyAsync(g => g.Code == code && g.Id != id, cancellationToken))
            throw new DomainException("کد گروه مشتری تکراری است.");

        group.Update(
            command.Name,
            command.Code,
            command.Description,
            command.IsWholesale,
            command.SortOrder,
            command.IsActive,
            command.MinimumOrderAmount,
            command.MinimumOrderQuantity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _db.CustomerGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerGroup), id);
        _db.CustomerGroups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignUserAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        if (!await _db.CustomerGroups.AnyAsync(g => g.Id == groupId, cancellationToken))
            throw new NotFoundException(nameof(CustomerGroup), groupId);

        var membership = await _db.CustomerGroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);

        if (membership is null)
        {
            _db.CustomerGroupMemberships.Add(CustomerGroupMembership.Create(userId, groupId));
        }
        else
        {
            membership.ChangeGroup(groupId);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateAsync(SaveCustomerGroupCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static CustomerGroupDto Map(CustomerGroup g) =>
        new(g.Id, g.Name, g.Code, g.Description, g.IsWholesale, g.SortOrder, g.IsActive,
            g.MinimumOrderAmount, g.MinimumOrderQuantity);
}

public sealed class PriceRuleService : IPriceRuleService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SavePriceRuleCommand> _validator;

    public PriceRuleService(ShopDbContext db, IValidator<SavePriceRuleCommand> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<IReadOnlyList<PriceRuleDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.PriceRules
            .AsNoTracking()
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.Name)
            .Select(r => Map(r))
            .ToListAsync(cancellationToken);

    public async Task<Guid> CreateAsync(SavePriceRuleCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureRefsAsync(command, cancellationToken);

        var rule = PriceRule.Create(
            command.Name,
            command.RuleType,
            command.ProductId,
            command.VariationId,
            command.CustomerGroupId,
            command.MinQuantity,
            command.MaxQuantity,
            command.FixedPrice,
            command.DiscountPercent,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.Priority);

        if (!command.IsActive)
            rule.Update(command.Name, command.RuleType, command.ProductId, command.VariationId, command.CustomerGroupId,
                command.MinQuantity, command.MaxQuantity, command.FixedPrice, command.DiscountPercent,
                command.StartsAtUtc, command.EndsAtUtc, false, command.Priority);

        _db.PriceRules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return rule.Id;
    }

    public async Task UpdateAsync(Guid id, SavePriceRuleCommand command, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await EnsureRefsAsync(command, cancellationToken);

        var rule = await _db.PriceRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PriceRule), id);

        rule.Update(
            command.Name,
            command.RuleType,
            command.ProductId,
            command.VariationId,
            command.CustomerGroupId,
            command.MinQuantity,
            command.MaxQuantity,
            command.FixedPrice,
            command.DiscountPercent,
            command.StartsAtUtc,
            command.EndsAtUtc,
            command.IsActive,
            command.Priority);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _db.PriceRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(PriceRule), id);
        _db.PriceRules.Remove(rule);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRefsAsync(SavePriceRuleCommand command, CancellationToken cancellationToken)
    {
        if (command.ProductId.HasValue &&
            !await _db.Products.AnyAsync(p => p.Id == command.ProductId.Value, cancellationToken))
            throw new NotFoundException(nameof(Product), command.ProductId.Value);

        if (command.CustomerGroupId.HasValue &&
            !await _db.CustomerGroups.AnyAsync(g => g.Id == command.CustomerGroupId.Value, cancellationToken))
            throw new NotFoundException(nameof(CustomerGroup), command.CustomerGroupId.Value);
    }

    private async Task ValidateAsync(SavePriceRuleCommand command, CancellationToken cancellationToken)
    {
        var result = await _validator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }
    }

    private static PriceRuleDto Map(PriceRule r) =>
        new(r.Id, r.Name, r.RuleType, r.ProductId, r.VariationId, r.CustomerGroupId,
            r.MinQuantity, r.MaxQuantity, r.FixedPrice, r.DiscountPercent,
            r.StartsAtUtc, r.EndsAtUtc, r.IsActive, r.Priority);
}
