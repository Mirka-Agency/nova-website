using CMS.Application.Users;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Identity;
using CMS.Web.Areas.Admin.ViewModels;
using CMS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UsersController> _logger;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<UsersController> logger,
        IStringLocalizer<AdminShared> localizer)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, string? q = null, string? role = null)
    {
        var pageSize = CMS.Application.Common.Paging.PagedRequest.DefaultPageSize;
        var normalizedPage = page < 1 ? 1 : page;
        var roleFilter = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        var query = _userManager.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.FullName != null && u.FullName.Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        if (roleFilter is not null)
        {
            if (await _roleManager.RoleExistsAsync(roleFilter))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(roleFilter);
                var userIds = usersInRole.Select(u => u.Id).ToList();
                query = query.Where(u => userIds.Contains(u.Id));
            }
            else
            {
                query = query.Where(_ => false);
            }
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                AvatarDisplayUrl = UserAvatarUrl.Resolve(user.AvatarUrl, user.Email, 64),
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                Roles = (await _userManager.GetRolesAsync(user))
                    .OrderBy(r => r)
                    .Select(RoleDisplayName)
                    .ToList(),
                IsLockedOut = await _userManager.IsLockedOutAsync(user)
            });
        }

        ViewBag.Page = normalizedPage;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Search = q;
        ViewBag.Role = roleFilter;
        ViewBag.RoleOptions = await GetRoleOptionsAsync(roleFilter is null ? [] : [roleFilter]);
        ViewBag.HasActiveFilters = !string.IsNullOrWhiteSpace(q) || roleFilter is not null;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildCreateModelAsync(new CreateUserViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        model = await BuildCreateModelAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing is not null)
        {
            ModelState.AddModelError(nameof(model.Email), _localizer["EmailExists"]);
            return View(model);
        }

        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim(),
            PhoneNumber = phone,
            PhoneNumberConfirmed = phone is not null
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roles = await NormalizeSelectedRolesAsync(model.SelectedRoles);
        if (roles.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, roles);
        }

        _logger.LogInformation("Admin action: user {Actor} created user {UserId} ({Email})",
            User.Identity?.Name, user.Id, user.Email);
        TempData["Success"] = _localizer["UserCreated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            SelectedRoles = (await _userManager.GetRolesAsync(user)).ToList(),
            IsLockedOut = await _userManager.IsLockedOutAsync(user)
        };

        return View(await BuildEditModelAsync(model));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        model = await BuildEditModelAsync(model);

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var selectedRoles = await NormalizeSelectedRolesAsync(model.SelectedRoles);
        var currentRoles = await _userManager.GetRolesAsync(user);

        if (!await CanChangeAdminRoleAsync(user, currentRoles, selectedRoles))
        {
            ModelState.AddModelError(string.Empty, _localizer["LastAdminRole"]);
            return View(model);
        }

        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.FullName = model.FullName;
        user.AvatarUrl = string.IsNullOrWhiteSpace(model.AvatarUrl) ? null : model.AvatarUrl.Trim();
        if (!string.Equals(user.PhoneNumber, phone, StringComparison.Ordinal))
        {
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = phone is not null;
        }
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult);
            return View(model);
        }

        var toRemove = currentRoles.Except(selectedRoles).ToList();
        var toAdd = selectedRoles.Except(currentRoles).ToList();

        if (toRemove.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, toRemove);
        }

        if (toAdd.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, toAdd);
        }

        await _userManager.SetLockoutEnabledAsync(user, true);
        if (model.IsLockedOut)
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        _logger.LogInformation("Admin action: user {Actor} updated user {UserId}",
            User.Identity?.Name, user.Id);
        TempData["Success"] = _localizer["UserUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new AdminSetUserPasswordViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetPassword(AdminSetUserPasswordViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
        {
            return NotFound();
        }

        model.Email = user.Email ?? string.Empty;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        _logger.LogInformation("Admin action: user {Actor} set password for user {UserId}",
            User.Identity?.Name, user.Id);
        TempData["Success"] = _localizer["UserPasswordChanged"].Value;
        return RedirectToAction(nameof(Edit), new { id = user.Id });
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            var message = IdentityErrorLocalizer.Localize(error);

            var key = error.Code is "DuplicateUserName" or "DuplicateEmail"
                ? nameof(CreateUserViewModel.Email)
                : string.Empty;

            ModelState.AddModelError(key, message);
        }
    }

    private async Task<bool> CanChangeAdminRoleAsync(
        ApplicationUser user,
        IList<string> currentRoles,
        IReadOnlyCollection<string> selectedRoles)
    {
        var hadAdmin = currentRoles.Contains(AuthRoles.Admin);
        var keepsAdmin = selectedRoles.Contains(AuthRoles.Admin);
        if (!hadAdmin || keepsAdmin)
        {
            return true;
        }

        var admins = await _userManager.GetUsersInRoleAsync(AuthRoles.Admin);
        var otherAdmins = admins.Count(a => a.Id != user.Id && !(a.LockoutEnd.HasValue && a.LockoutEnd > DateTimeOffset.UtcNow));
        return otherAdmins > 0;
    }

    private async Task<List<string>> NormalizeSelectedRolesAsync(IEnumerable<string>? selected)
    {
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
        return (selected ?? [])
            .Where(r => allRoles.Contains(r))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<CreateUserViewModel> BuildCreateModelAsync(CreateUserViewModel model)
    {
        model.AvailableRoles = await GetRoleOptionsAsync(model.SelectedRoles);
        return model;
    }

    private async Task<EditUserViewModel> BuildEditModelAsync(EditUserViewModel model)
    {
        model.AvailableRoles = await GetRoleOptionsAsync(model.SelectedRoles);
        return model;
    }

    private async Task<IEnumerable<SelectListItem>> GetRoleOptionsAsync(IEnumerable<string> selected)
    {
        var selectedSet = selected.ToHashSet(StringComparer.Ordinal);
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        return roles.Select(r => new SelectListItem
        {
            Text = RoleDisplayName(r.Name),
            Value = r.Name,
            Selected = selectedSet.Contains(r.Name!)
        });
    }

    private string RoleDisplayName(string? roleName) => roleName switch
    {
        AuthRoles.Admin => _localizer["Role_Admin"].Value,
        AuthRoles.Editor => _localizer["Role_Editor"].Value,
        AuthRoles.ShopManager => _localizer["Role_ShopManager"].Value,
        AuthRoles.Viewer => _localizer["Role_Viewer"].Value,
        _ => roleName ?? string.Empty
    };
}
