using Microsoft.AspNetCore.Mvc;

namespace CMS.Web.Controllers;

/// <summary>Permanent redirects from the old /contact URL to /contact-us.</summary>
[Route("contact")]
public class ContactLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/contact-us/");
}
