using Microsoft.AspNetCore.Mvc;

namespace CMS.Modules.News.Web.Controllers;

/// <summary>Permanent redirects from the old /events URLs to /event.</summary>
[Route("events")]
public class EventsLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/event");

    [HttpGet("{slug}")]
    public IActionResult Details(string slug) => RedirectPermanent($"/event/{slug}");
}
