using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

/// <summary>Permanent redirects from the old /about URL to /about-us.</summary>
[Route("about")]
public class AboutLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/about-us/");
}
