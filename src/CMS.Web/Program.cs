using System.Globalization;
using CMS.Application;
using CMS.Infrastructure;
using CMS.Infrastructure.Features;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Settings;
using CMS.Modules.Blog.Infrastructure;
using CMS.Modules.Blog.Infrastructure.Persistence;
using CMS.Modules.News.Infrastructure;
using CMS.Modules.News.Infrastructure.Persistence;
using CMS.Modules.Services.Infrastructure;
using CMS.Modules.Services.Infrastructure.Persistence;
using CMS.Modules.Video.Infrastructure;
using CMS.Modules.Video.Infrastructure.Persistence;
using CMS.Modules.Team.Infrastructure;
using CMS.Modules.Team.Infrastructure.Persistence;
using CMS.Modules.Honors.Infrastructure;
using CMS.Modules.Honors.Infrastructure.Persistence;
using CMS.Modules.Voices.Infrastructure;
using CMS.Modules.Voices.Infrastructure.Persistence;
using CMS.Modules.Forms.Infrastructure;
using CMS.Modules.Forms.Infrastructure.Persistence;
using CMS.Modules.Forms.Infrastructure.Services;
using CMS.Modules.Media.Infrastructure;
using CMS.Modules.Media.Infrastructure.Persistence;
using CMS.Modules.Comments.Infrastructure;
using CMS.Modules.Comments.Infrastructure.Persistence;
using CMS.Modules.Popup.Infrastructure;
using CMS.Modules.Popup.Infrastructure.Persistence;
using CMS.Modules.Seo.Infrastructure;
using CMS.Modules.Seo.Infrastructure.Middleware;
using CMS.Modules.Seo.Infrastructure.Persistence;
using CMS.Modules.Shop.Infrastructure;
using CMS.Modules.Shop.Infrastructure.Persistence;
using CMS.Web.Middleware;
using CMS.Web.Observability;
using CMS.Web.Security;
using CMS.Web.Validation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Make repo-root `.env` available for local `dotnet run` (VS Code already uses envFile).
    CMS.Web.EnvFileLoader.LoadNearest();

    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsProduction())
    {
        var allowedHosts = builder.Configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException(
                "AllowedHosts must be set explicitly in Production (not '*'). " +
                "Example: AllowedHosts=www.example.com;example.com");
        }
    }

    // Ensure process defaults match the Farsi-first UI (avoids invariant-culture fallbacks).
    var faIr = new CultureInfo("fa-IR");
    CultureInfo.DefaultThreadCurrentCulture = faIr;
    CultureInfo.DefaultThreadCurrentUICulture = faIr;

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CMS.Web"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddBlogModule(builder.Configuration);
    builder.Services.AddNewsModule(builder.Configuration);
    builder.Services.AddServicesModule(builder.Configuration);
    builder.Services.AddVideoModule(builder.Configuration);
    builder.Services.AddTeamModule(builder.Configuration);
    builder.Services.AddHonorsModule(builder.Configuration);
    builder.Services.AddVoicesModule(builder.Configuration);
    builder.Services.AddShopModule(builder.Configuration);
    builder.Services.AddFormsModule(builder.Configuration);
    builder.Services.AddMediaModule(builder.Configuration);
    builder.Services.AddCommentsModule(builder.Configuration);
    builder.Services.AddPopupModule(builder.Configuration);
    builder.Services.AddSeoModule(builder.Configuration);
    builder.Services.AddCmsRateLimiting();
    builder.Services.AddCmsHealthChecks(builder.Configuration);
    builder.Services.AddCmsOpenTelemetry(builder.Configuration, builder.Environment);
    builder.Services.AddResponseCaching();
    builder.Services.AddHttpClient("MediaContent", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60);
    });

    var sessionSecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;

    builder.Services.AddSession(options =>
    {
        options.Cookie.Name = ".Mirka.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = sessionSecurePolicy;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.IdleTimeout = TimeSpan.FromHours(8);
    });

    builder.Services
        .AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add<AdminMutationAuthorizationFilter>();
            PersianModelBindingMessages.Apply(options.ModelBindingMessageProvider);
        })
        .AddViewLocalization()
        .AddDataAnnotationsLocalization(options =>
        {
            options.DataAnnotationLocalizerProvider = (_, factory) =>
                factory.Create(typeof(CMS.Web.AdminShared));
        });

    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    // CapRover / nginx terminate TLS and send X-Forwarded-Proto. Trust the proxy so
    // HTTPS redirection and Secure cookies work behind the load balancer.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var culture = new CultureInfo("fa-IR");
        options.DefaultRequestCulture = new RequestCulture(culture);
        options.SupportedCultures = [culture];
        options.SupportedUICultures = [culture];
        options.ApplyCurrentCultureToResponseHeaders = true;
    });

    var app = builder.Build();

    var migrateOnStartup = app.Configuration.GetValue("Database:MigrateOnStartup", true);
    if (migrateOnStartup)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync();
            var blogDb = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            await blogDb.Database.MigrateAsync();
            var newsDb = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
            await newsDb.Database.MigrateAsync();
            var servicesDb = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();
            await servicesDb.Database.MigrateAsync();
            var videoDb = scope.ServiceProvider.GetRequiredService<VideoDbContext>();
            await videoDb.Database.MigrateAsync();
            var teamDb = scope.ServiceProvider.GetRequiredService<TeamDbContext>();
            await teamDb.Database.MigrateAsync();
            var honorsDb = scope.ServiceProvider.GetRequiredService<HonorsDbContext>();
            await honorsDb.Database.MigrateAsync();
            await HonorDefaultSeeder.SeedAsync(app.Services);
            var voicesDb = scope.ServiceProvider.GetRequiredService<VoicesDbContext>();
            await voicesDb.Database.MigrateAsync();
            var formsDb = scope.ServiceProvider.GetRequiredService<FormsDbContext>();
            await formsDb.Database.MigrateAsync();
            await FormEnginePhase1Seeder.SeedAsync(app.Services);
            await FormEnginePhase3Seeder.SeedAsync(app.Services);
            await FormSystemSeeder.SeedAsync(app.Services);
            var shopDb = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            await shopDb.Database.MigrateAsync();
            var customerGroups = scope.ServiceProvider.GetRequiredService<CMS.Modules.Shop.Application.Interfaces.ICustomerGroupService>();
            await customerGroups.EnsureDefaultsAsync();
            var iranLocations = scope.ServiceProvider.GetRequiredService<CMS.Modules.Shop.Application.Locations.IIranLocationService>();
            await iranLocations.EnsureSeededAsync();
            var mediaDb = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await mediaDb.Database.MigrateAsync();
            var commentsDb = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
            await commentsDb.Database.MigrateAsync();
            var popupDb = scope.ServiceProvider.GetRequiredService<PopupDbContext>();
            await popupDb.Database.MigrateAsync();
            await PopupDefaultSeeder.SeedAsync(app.Services);
            await CMS.Web.Seeding.PopupBookingSeeder.SeedAsync(app.Services);
            var seoDb = scope.ServiceProvider.GetRequiredService<SeoDbContext>();
            await seoDb.Database.MigrateAsync();
            await IdentityDataSeeder.SeedAsync(app.Services);
            await FeatureToggleSeeder.SeedAsync(scope.ServiceProvider);
            await SiteSettingsSeeder.SeedAsync(app.Services);
            Log.Information("Database migrated and identity/feature seed completed");
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "28P01")
        {
            Log.Fatal(ex, "PostgreSQL authentication failed for connection string {Name}",
                DatabaseConstants.ConnectionStringName);
            throw new InvalidOperationException(
                "PostgreSQL authentication failed. Set ConnectionStrings:DefaultConnection to your real credentials, e.g.\n" +
                "  dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=localhost;Port=5432;Database=mirka_cms;Username=postgres;Password=YOUR_PASSWORD\" --project src/CMS.Web\n" +
                "Or edit appsettings.Development.json / environment variable ConnectionStrings__DefaultConnection.",
                ex);
        }
    }
    else
    {
        Log.Warning("Database:MigrateOnStartup is false; skipping EF migrations. Module tables (e.g. shop.Settings) will be missing until you migrate.");
    }

    app.UseForwardedHeaders();
    app.UseGlobalExceptionHandling();
    app.UseSecurityHeaders();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    if (!app.Environment.IsEnvironment("Testing"))
        app.UseHttpsRedirection();

    app.UseSerilogRequestLogging();
    app.UseRequestLocalization();
    app.UseRouting();
    app.UseSeoRedirects();
    app.UseStaffResponseCacheBypass();
    app.UseResponseCaching();
    app.UseRateLimiter();
    app.UseSession();
    app.UseAuthentication();
    app.UseMiddleware<CMS.Web.Middleware.CustomerPrincipalMiddleware>();
    app.UseAuthorization();
    app.UseMaintenanceMode();
    app.MapStaticAssets();

    app.MapCmsHealthEndpoints();

    app.MapControllers()
        .RequireRateLimiting(RateLimitingConfiguration.GlobalPolicy);

    app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
        .RequireRateLimiting(RateLimitingConfiguration.GlobalPolicy);

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .RequireRateLimiting(RateLimitingConfiguration.GlobalPolicy)
        .WithStaticAssets();

    Log.Information("CMS.Web starting in {Environment}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CMS.Web terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
