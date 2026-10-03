using CMS.Application.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.ViewComponents;

public sealed class AccountNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var authenticated = User?.Identity?.IsAuthenticated == true
            && string.Equals(User.Identity.AuthenticationType, CustomerAuthDefaults.AuthenticationScheme, StringComparison.Ordinal);

        return View(authenticated);
    }
}
