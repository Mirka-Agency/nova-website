using CMS.Application.Common.Features;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Features;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CMS.Web.Integration.Tests;

[CollectionDefinition(Name)]
public sealed class CmsWebCollection : ICollectionFixture<CmsWebFixture>
{
    public const string Name = "CmsWeb";
}

public sealed class CmsWebFixture : IAsyncLifetime
{
    public CmsWebApplicationFactory Factory { get; private set; } = null!;
    public bool IsAvailable { get; private set; }
    public string? SkipReason { get; private set; }

    public const string AdminEmail = "admin@local.test";
    public const string AdminPassword = "Admin123!";
    public const string EditorEmail = "editor@local.test";
    public const string EditorPassword = "Editor123!";
    public const string ShopManagerEmail = "shop@local.test";
    public const string ShopManagerPassword = "Shop1234!";
    public const string ViewerEmail = "viewer@local.test";
    public const string ViewerPassword = "Viewer123!";

    public async Task InitializeAsync()
    {
        try
        {
            var connectionString = TestConnectionString.Resolve();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                IsAvailable = false;
                SkipReason =
                    "No usable Postgres connection. Set ConnectionStrings__DefaultConnection " +
                    "or configure CMS.Web user-secrets, then re-run. Tests use database mirka_cms_test.";
                return;
            }

            await EnsureDatabaseExistsAsync(connectionString);

            // Ensure env wins inside WebApplication.CreateBuilder configuration.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);

            Factory = new CmsWebApplicationFactory(connectionString);
            using var client = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            _ = await client.GetAsync("/");

            await using var scope = Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await db.Database.CanConnectAsync())
            {
                IsAvailable = false;
                SkipReason = "PostgreSQL is not reachable after factory start.";
                return;
            }

            await EnsureFeatureAsync(db, FeatureNames.Shop, enabled: true);
            await EnsureFeatureAsync(db, FeatureNames.Blog, enabled: true);
            await EnsureFeatureAsync(db, FeatureNames.News, enabled: true);
            await EnsureUserAsync(scope.ServiceProvider, EditorEmail, EditorPassword, AuthRoles.Editor);
            await EnsureUserAsync(scope.ServiceProvider, ShopManagerEmail, ShopManagerPassword, AuthRoles.ShopManager);
            await EnsureUserAsync(scope.ServiceProvider, ViewerEmail, ViewerPassword, AuthRoles.Viewer);

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            SkipReason =
                "Integration host could not start (usually PostgreSQL / connection string). " +
                "Set ConnectionStrings__DefaultConnection; tests use database mirka_cms_test. " +
                $"Details: {ex.GetType().Name}: {ex.Message}";
        }
    }

    public Task DisposeAsync()
    {
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    public void EnsureAvailable()
    {
        Skip.If(!IsAvailable, SkipReason ?? "PostgreSQL test database is unavailable.");
    }

    public async Task SetShopFeatureAsync(bool enabled)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureFeatureAsync(db, FeatureNames.Shop, enabled);
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var database = builder.Database;
        if (string.IsNullOrWhiteSpace(database))
            return;

        builder.Database = "postgres";
        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using (var existsCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", conn))
        {
            existsCmd.Parameters.AddWithValue("name", database);
            var exists = await existsCmd.ExecuteScalarAsync();
            if (exists is not null)
                return;
        }

        await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", conn);
        await createCmd.ExecuteNonQueryAsync();
    }

    private static async Task EnsureFeatureAsync(ApplicationDbContext db, string name, bool enabled)
    {
        var toggle = await db.FeatureToggles.FirstOrDefaultAsync(t => t.Name == name);
        if (toggle is null)
        {
            db.FeatureToggles.Add(new FeatureToggle
            {
                Name = name,
                Enabled = enabled,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            toggle.Enabled = enabled;
            toggle.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        IServiceProvider services,
        string email,
        string password,
        string role)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = email
            };
            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
                throw new InvalidOperationException(string.Join(", ", create.Errors.Select(e => e.Description)));
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
