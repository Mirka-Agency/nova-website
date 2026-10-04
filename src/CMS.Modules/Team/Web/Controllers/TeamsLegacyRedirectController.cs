using Microsoft.AspNetCore.Mvc;

namespace CMS.Modules.Team.Web.Controllers;

/// <summary>Permanent redirects from the old /Teams URLs to /doctors.</summary>
[Route("Teams")]
public class TeamsLegacyRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => RedirectPermanent("/doctors");

    [HttpGet("{slug}")]
    public IActionResult Details(string slug) => RedirectPermanent($"/doctors/{slug}");
}
