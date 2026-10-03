using CMS.Application.Audit;
using CMS.Application.Settings;
using CMS.Domain.Exceptions;
using CMS.Infrastructure.Auth;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class SiteScriptsController : Controller
{
    private readonly ISiteSettingsService _settings;
    private readonly IAuditLogger _audit;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public SiteScriptsController(
        ISiteSettingsService settings,
        IAuditLogger audit,
        IStringLocalizer<AdminShared> localizer)
    {
        _settings = settings;
        _audit = audit;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["SiteScripts"].Value;
        var dto = await _settings.GetAsync(cancellationToken);
        return View(new SiteScriptsFormViewModel
        {
            HeadScripts = dto.HeadScripts,
            BodyOpenScripts = dto.BodyOpenScripts,
            BodyCloseScripts = dto.BodyCloseScripts
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(SiteScriptsFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = _localizer["SiteScripts"].Value;
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _settings.UpdateScriptsAsync(
                new UpdateSiteScriptsCommand(
                    model.HeadScripts,
                    model.BodyOpenScripts,
                    model.BodyCloseScripts),
                cancellationToken);
            await _audit.LogAsync("Update", "SiteScripts", cancellationToken: cancellationToken);
            TempData["Success"] = _localizer["SiteScriptsUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (DomainValidationException ex)
        {
            foreach (var (key, messages) in ex.Errors)
                foreach (var message in messages)
                    ModelState.AddModelError(key, message);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}
