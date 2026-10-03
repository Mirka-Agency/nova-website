using CMS.Application.Users;
using CMS.Infrastructure.Identity;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Areas.Admin.ViewComponents;

public class AdminUserMenuViewComponent : ViewComponent
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserMenuViewComponent(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
        if (user is null)
        {
            return Content(string.Empty);
        }

        var email = user.Email ?? user.UserName ?? string.Empty;
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? email : user.FullName.Trim();

        return View(new AdminUserMenuViewModel
        {
            Email = email,
            DisplayName = displayName,
            Initials = BuildInitials(displayName, email),
            AvatarUrl = UserAvatarUrl.Resolve(user.AvatarUrl, email, 72)
        });
    }

    private static string BuildInitials(string displayName, string email)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? email : displayName;
        var parts = source
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 2)
        {
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        if (parts.Length == 1 && parts[0].Length >= 2)
        {
            return parts[0][..2].ToUpperInvariant();
        }

        return source.Length > 0 ? char.ToUpperInvariant(source[0]).ToString() : "?";
    }
}
