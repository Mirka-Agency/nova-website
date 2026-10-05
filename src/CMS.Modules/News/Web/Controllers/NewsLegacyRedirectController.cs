using Microsoft.AspNetCore.Mvc;

namespace CMS.Modules.News.Web.Controllers;

/// <summary>Permanent redirects from the old /news URLs to /education-articles.</summary>
[Route("news")]
public class NewsLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/education-articles");

    [HttpGet("{slug}")]
    public IActionResult Details(string slug) => RedirectPermanent($"/education-articles/{slug}");
}
