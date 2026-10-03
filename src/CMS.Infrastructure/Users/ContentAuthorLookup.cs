using CMS.Application.Users;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMS.Infrastructure.Users;

public sealed class ContentAuthorLookup : IContentAuthorLookup
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ContentAuthorLookup(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ContentAuthorOptionDto>> ListContentAuthorsAsync(
        CancellationToken cancellationToken = default)
    {
        var admins = await _userManager.GetUsersInRoleAsync(AuthRoles.Admin);
        var editors = await _userManager.GetUsersInRoleAsync(AuthRoles.Editor);

        return admins
            .Concat(editors)
            .GroupBy(u => u.Id)
            .Select(g => g.First())
            .OrderBy(u => DisplayName(u), StringComparer.OrdinalIgnoreCase)
            .Select(u => new ContentAuthorOptionDto(u.Id, DisplayName(u)))
            .ToList();
    }

    public async Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : DisplayName(user);
    }

    private static string DisplayName(ApplicationUser user) =>
        !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName.Trim()
            : (user.Email ?? user.UserName ?? user.Id);
}
