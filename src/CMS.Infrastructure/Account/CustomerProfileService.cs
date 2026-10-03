using CMS.Application.Account;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Account;

public sealed class CustomerProfileService : ICustomerProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<CustomerProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task UpdateAsync(string userId, UpdateCustomerProfileCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var fullName = command.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("نام و نام خانوادگی الزامی است.");

        user.FullName = fullName;

        var email = command.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            user.Email = $"{user.PhoneNumber ?? user.UserName}@customers.local";
            user.EmailConfirmed = false;
        }
        else
        {
            var normalizedEmail = _userManager.NormalizeEmail(email);
            var exists = await _userManager.Users.AnyAsync(
                u => u.NormalizedEmail == normalizedEmail && u.Id != userId,
                cancellationToken);
            if (exists)
                throw new DomainException("این ایمیل قبلاً ثبت شده است.");

            user.Email = email;
            user.EmailConfirmed = false;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    public async Task<bool> IsProfileCompleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.IsProfileComplete == true;
    }

    private static CustomerProfileDto Map(ApplicationUser user)
    {
        var email = user.Email;
        if (!string.IsNullOrWhiteSpace(email) &&
            email.EndsWith("@customers.local", StringComparison.OrdinalIgnoreCase))
        {
            email = null;
        }

        return new CustomerProfileDto(
            user.Id,
            user.PhoneNumber ?? user.UserName ?? string.Empty,
            user.FullName,
            email,
            user.IsProfileComplete);
    }
}
