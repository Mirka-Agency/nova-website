using CMS.Modules.Shop.Domain.Entities;

namespace CMS.Modules.Shop.Application.Account;

public sealed record CustomerAddressDto(
    Guid Id,
    string RecipientName,
    string RecipientPhone,
    string Province,
    string City,
    string? PostalCode,
    string AddressLine,
    bool IsDefault);

public sealed record SaveCustomerAddressCommand(
    string RecipientName,
    string RecipientPhone,
    string Province,
    string City,
    string? PostalCode,
    string AddressLine,
    bool IsDefault);

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> ListAsync(string userId, CancellationToken cancellationToken = default);
    Task<CustomerAddressDto?> GetAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(string userId, SaveCustomerAddressCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(string userId, Guid id, SaveCustomerAddressCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, Guid id, CancellationToken cancellationToken = default);
}
