using CMS.Application.Audit;
using CMS.Application.Editing;
using CMS.Application.Email;
using CMS.Application.Messaging;
using CMS.Application.Payments;
using CMS.Application.Security;
using CMS.Application.Seo;
using CMS.Application.Settings;
using CMS.Application.Sms;
using CMS.Application.Storage;
using CMS.Application.Users;
using CMS.Infrastructure.Audit;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Cache;
using CMS.Infrastructure.Editing;
using CMS.Infrastructure.Email;
using CMS.Infrastructure.Features;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Messaging;
using CMS.Infrastructure.Payments;
using CMS.Infrastructure.Payments.Providers;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.Security;
using CMS.Infrastructure.Seo;
using CMS.Infrastructure.Settings;
using CMS.Infrastructure.Sms;
using CMS.Infrastructure.Storage;
using CMS.Infrastructure.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace CMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");

        var cookieSecurePolicy = environment.IsProduction()
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;

        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddCmsCacheAndDataProtection(configuration, environment);
        services.Configure<SeedAdminOptions>(configuration.GetSection(SeedAdminOptions.SectionName));
        services.Configure<S3ObjectStorageOptions>(configuration.GetSection(S3ObjectStorageOptions.SectionName));
        services.Configure<SmtpEmailOptions>(configuration.GetSection(SmtpEmailOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
        services.Configure<SmsIrOptions>(configuration.GetSection(SmsIrOptions.SectionName));

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        services.AddSingleton<IHtmlContentSanitizer, HtmlContentSanitizer>();
        services.Configure<SharedCaptchaOptions>(configuration.GetSection(SharedCaptchaOptions.SectionName));
        services.AddHttpClient(SharedCaptchaVerifier.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<ISharedCaptchaCredentials, SharedCaptchaCredentials>();
        services.AddSingleton<ISharedCaptchaVerifier, SharedCaptchaVerifier>();
        services.Configure<AdminLoginCaptchaOptions>(configuration.GetSection(AdminLoginCaptchaOptions.SectionName));
        services.AddSingleton<IAdminLoginCaptchaService, AdminLoginCaptchaService>();
        services.AddSingleton<IMessageLogger, MessageLogger>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        services.AddSingleton<ISmsProvider, SmsIrSmsProvider>();
        services.AddSingleton<ISmsSender, SmsSender>();
        services.AddSingleton<CMS.Infrastructure.Notifications.AdminNotifier>();
        services.AddSingleton<IAdminSmsNotifier>(sp => sp.GetRequiredService<CMS.Infrastructure.Notifications.AdminNotifier>());
        services.AddSingleton<IAdminNotifier>(sp => sp.GetRequiredService<CMS.Infrastructure.Notifications.AdminNotifier>());

        services.AddScoped<IPaymentGateway, ManualPaymentGateway>();
        services.AddSingleton<IPaymentSettingsProtector, DataProtectionPaymentSettingsProtector>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentProvider, ZarinpalPaymentProvider>();
        services.AddScoped<IPaymentProvider, ZibalPaymentProvider>();
        services.AddScoped<IPaymentProvider, SepPaymentProvider>();
        services.AddScoped<IPaymentProvider, MellatPaymentProvider>();
        services.AddScoped<IPaymentProvider, SnapPayPaymentProvider>();
        services.AddHttpClient(nameof(ZarinpalPaymentProvider));
        services.AddHttpClient(nameof(ZibalPaymentProvider));
        services.AddHttpClient(nameof(SepPaymentProvider));
        services.AddHttpClient(nameof(MellatPaymentProvider));
        services.AddHttpClient(nameof(SnapPayPaymentProvider));
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<CMS.Application.Background.IBackgroundTaskQueue, CMS.Application.Background.BackgroundTaskQueue>();
        services.AddHostedService<CMS.Infrastructure.Background.BackgroundTaskHostedService>();
        services.AddScoped<IAuditLogger, AuditService>();
        services.AddScoped<IAuditQueryService, AuditService>();
        services.AddScoped<IMessageLogQueryService, MessageLogQueryService>();
        services.AddScoped<IContentAuthorLookup, ContentAuthorLookup>();
        services.AddScoped<IUserAccountLookup, UserAccountLookup>();
        services.AddSingleton<IEditLockService, EditLockService>();
        services.AddScoped<IAdminEditLockAccessor, AdminEditLockAccessor>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), errorCodesToAdd: null)));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/admin/account/login";
            options.AccessDeniedPath = "/admin/account/accessdenied";
            options.LogoutPath = "/admin/account/logout";
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Cookie.Name = CMS.Application.Auth.StaffAuthDefaults.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        services.AddAuthentication()
            .AddCookie(CMS.Application.Auth.CustomerAuthDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = CMS.Application.Auth.CustomerAuthDefaults.LoginPath;
                options.AccessDeniedPath = CMS.Application.Auth.CustomerAuthDefaults.LoginPath;
                options.LogoutPath = CMS.Application.Auth.CustomerAuthDefaults.LogoutPath;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.Cookie.Name = CMS.Application.Auth.CustomerAuthDefaults.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = cookieSecurePolicy;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        services.Configure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
        {
            options.HeaderName = "RequestVerificationToken";
            options.Cookie.Name = "CMS.Admin.AF";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddFeatureManagement();
        services.RemoveAll<IFeatureDefinitionProvider>();
        services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();

        services.AddScoped<ISitemapService, SitemapService>();
        services.AddScoped<ISitemapUrlProvider, StaticPagesSitemapUrlProvider>();

        services.AddScoped<CMS.Application.Account.ICustomerAuthService, CMS.Infrastructure.Account.CustomerAuthService>();
        services.AddScoped<CMS.Application.Account.ICustomerProfileService, CMS.Infrastructure.Account.CustomerProfileService>();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(AuthRoles.Admin))
            .AddPolicy(AuthPolicies.ManageBlog, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageNews, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageServices, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageVideo, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageTeam, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageHonors, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageVoices, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageShop, p => p.RequireRole(AuthRoles.Admin, AuthRoles.ShopManager))
            .AddPolicy(AuthPolicies.ManageForms, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageFormSubmissions, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ExportFormSubmissions, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageMedia, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.ShopManager))
            .AddPolicy(AuthPolicies.ManageComments, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManagePopup, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ManageSeo, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor))
            .AddPolicy(AuthPolicies.ViewBlog, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewNews, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewServices, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewVideo, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewTeam, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewHonors, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewVoices, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewShop, p => p.RequireRole(AuthRoles.Admin, AuthRoles.ShopManager, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewForms, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewMedia, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.ShopManager, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewComments, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewPopup, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.ViewSeo, p => p.RequireRole(AuthRoles.Admin, AuthRoles.Editor, AuthRoles.Viewer))
            .AddPolicy(AuthPolicies.Customer, p =>
            {
                p.AddAuthenticationSchemes(CMS.Application.Auth.CustomerAuthDefaults.AuthenticationScheme);
                p.RequireAuthenticatedUser();
            });

        return services;
    }
}
