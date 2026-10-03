namespace CMS.Application.Account;

public interface ICustomerProfileService
{
    Task<CustomerProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(string userId, UpdateCustomerProfileCommand command, CancellationToken cancellationToken = default);
    Task<bool> IsProfileCompleteAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed record CustomerProfileDto(
    string Id,
    string Phone,
    string? FullName,
    string? Email,
    bool IsProfileComplete);

public sealed record UpdateCustomerProfileCommand(string FullName, string? Email);
