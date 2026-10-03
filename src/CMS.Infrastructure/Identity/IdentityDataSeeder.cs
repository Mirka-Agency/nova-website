using CMS.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Infrastructure.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<SeedAdminOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentityDataSeeder");

        string[] roles =
        [
            AuthRoles.Admin,
            AuthRoles.Editor,
            AuthRoles.ShopManager,
            AuthRoles.Viewer
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {Role}: {Errors}",
                        role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        if (string.IsNullOrWhiteSpace(seedOptions.Password))
        {
            logger.LogWarning("Seed:Admin:Password is empty; skipping admin user seed.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(seedOptions.Email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = seedOptions.Email,
                Email = seedOptions.Email,
                EmailConfirmed = true,
                FullName = seedOptions.FullName
            };

            var createResult = await userManager.CreateAsync(admin, seedOptions.Password);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create seed admin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        else if (seedOptions.SyncPasswordOnStartup)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            var resetResult = await userManager.ResetPasswordAsync(admin, token, seedOptions.Password);
            if (!resetResult.Succeeded)
            {
                logger.LogError("Failed to sync seed admin password: {Errors}",
                    string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
            else
            {
                await userManager.SetLockoutEndDateAsync(admin, null);
                await userManager.ResetAccessFailedCountAsync(admin);
                logger.LogInformation("Seed admin password synced for {Email}", seedOptions.Email);
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AuthRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, AuthRoles.Admin);
        }
    }
}
