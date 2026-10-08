using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

/// <summary>Permanent redirects from the old /Home/Privacy URL to /privacy.</summary>
[Route("Home/Privacy")]
public class PrivacyLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/privacy/");
}
