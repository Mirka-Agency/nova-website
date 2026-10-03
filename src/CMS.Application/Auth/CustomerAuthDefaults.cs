namespace CMS.Application.Auth;

public static class CustomerAuthDefaults
{
    public const string AuthenticationScheme = "Customer";
    public const string CookieName = "CMS.Customer.Auth";
    public const string LoginPath = "/account/login";
    public const string LogoutPath = "/account/logout";
    public const string PolicyName = "Customer";
}
