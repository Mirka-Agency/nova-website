using CMS.Application.Auth;

namespace CMS.Infrastructure.Auth;

// Re-export for Infrastructure consumers that already import this namespace.
public static class CustomerAuthDefaults
{
    public const string AuthenticationScheme = CMS.Application.Auth.CustomerAuthDefaults.AuthenticationScheme;
    public const string CookieName = CMS.Application.Auth.CustomerAuthDefaults.CookieName;
    public const string LoginPath = CMS.Application.Auth.CustomerAuthDefaults.LoginPath;
    public const string LogoutPath = CMS.Application.Auth.CustomerAuthDefaults.LogoutPath;
}

