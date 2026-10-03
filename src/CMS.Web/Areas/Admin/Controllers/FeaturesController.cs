using CMS.Application.Common.Features;
using CMS.Infrastructure.Auth;
using CMS.Infrastructure.Features;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class FeaturesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IStringLocalizer<AdminShared> _localizer;
    private readonly ILogger<FeaturesController> _logger;

    public FeaturesController(
        ApplicationDbContext db,
        IMemoryCache cache,
        IStringLocalizer<AdminShared> localizer,
        ILogger<FeaturesController> logger)
    {
        _db = db;
        _cache = cache;
        _localizer = localizer;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = _localizer["FeaturesTitle"].Value;
        var items = await _db.FeatureToggles
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(string name)
    {
        if (name is not (FeatureNames.Blog or FeatureNames.News or FeatureNames.Services or FeatureNames.Video or FeatureNames.Team or FeatureNames.Honors or FeatureNames.Shop or FeatureNames.Forms or FeatureNames.Comments or FeatureNames.Popup or FeatureNames.Seo))
        {
            return NotFound();
        }

        var toggle = await _db.FeatureToggles.FirstOrDefaultAsync(t => t.Name == name);
        if (toggle is null)
        {
            toggle = new FeatureToggle { Name = name, Enabled = true };
            _db.FeatureToggles.Add(toggle);
        }
        else
        {
            toggle.Enabled = !toggle.Enabled;
        }

        toggle.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _cache.Remove(DatabaseFeatureDefinitionProvider.ToggleCacheKey);

        _logger.LogInformation(
            "Admin action: user {Actor} set feature {Feature} = {Enabled}",
            User.Identity?.Name, name, toggle.Enabled);

        TempData["Success"] = _localizer["FeatureUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }
}
