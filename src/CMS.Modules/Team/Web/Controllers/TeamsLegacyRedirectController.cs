using Microsoft.AspNetCore.Mvc;

namespace CMS.Modules.Team.Web.Controllers;

/// <summary>Permanent redirects from the old /teams (and /Teams via lowercase middleware) URLs to /doctors.</summary>
[Route("teams")]
public class TeamsLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/doctors/");

    [HttpGet("{slug}")]
    public IActionResult Details(string slug) => RedirectPermanent($"/doctors/{slug}/");
}
