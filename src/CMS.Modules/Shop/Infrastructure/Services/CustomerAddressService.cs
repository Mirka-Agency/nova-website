using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class CustomerAddressService : ICustomerAddressService
{
    private readonly ShopDbContext _db;

    public CustomerAddressService(ShopDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        await _db.CustomerAddresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Select(a => Map(a))
            .ToListAsync(cancellationToken);

    public async Task<CustomerAddressDto?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var address = await _db.CustomerAddresses.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        return address is null ? null : Map(address);
    }

    public async Task<Guid> CreateAsync(string userId, SaveCustomerAddressCommand command, CancellationToken cancellationToken = default)
    {
        if (command.IsDefault)
            await ClearDefaultsAsync(userId, cancellationToken);

        var hasAny = await _db.CustomerAddresses.AnyAsync(a => a.UserId == userId, cancellationToken);
        var address = CustomerAddress.Create(
            userId,
            command.RecipientName,
            command.RecipientPhone,
            command.Province,
            command.City,
            command.PostalCode,
            command.AddressLine,
            command.IsDefault || !hasAny);

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync(cancellationToken);
        return address.Id;
    }

    public async Task UpdateAsync(string userId, Guid id, SaveCustomerAddressCommand command, CancellationToken cancellationToken = default)
    {
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), id);

        if (command.IsDefault)
            await ClearDefaultsAsync(userId, cancellationToken);

        address.Update(
            command.RecipientName,
            command.RecipientPhone,
            command.Province,
            command.City,
            command.PostalCode,
            command.AddressLine,
            command.IsDefault);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default)
    {
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerAddress), id);
        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ClearDefaultsAsync(string userId, CancellationToken cancellationToken)
    {
        var defaults = await _db.CustomerAddresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);
        foreach (var item in defaults)
            item.SetDefault(false);
    }

    private static CustomerAddressDto Map(CustomerAddress a) =>
        new(a.Id, a.RecipientName, a.RecipientPhone, a.Province, a.City, a.PostalCode, a.AddressLine, a.IsDefault);
}
