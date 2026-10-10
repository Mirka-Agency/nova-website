using CMS.Application.Settings;
using CMS.Infrastructure.Auth;
using CMS.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public sealed class CtaHelperController : Controller
{
    private readonly ISiteSettingsService _siteSettings;
    private readonly IAuthorizationService _authorization;
    private readonly IStringLocalizer<CMS.Web.AdminShared> _localizer;

    public CtaHelperController(
        ISiteSettingsService siteSettings,
        IAuthorizationService authorization,
        IStringLocalizer<CMS.Web.AdminShared> localizer)
    {
        _siteSettings = siteSettings;
        _authorization = authorization;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync())
            return Forbid();

        var settings = await _siteSettings.GetAsync(cancellationToken);
        var contact = SiteContactInfo.From(settings);
        var doctorImage = Url.Content("~/template/assets/images/doctors/dr-hamedani.webp");

        ViewData["Title"] = _localizer["CtaHelper"].Value;

        return View(new CtaHelperPageViewModel
        {
            Brand = contact.Brand,
            PhoneDisplay = contact.PhoneDisplay ?? "۰۲۱-۹۱۰۹۳۴۹۲",
            PhoneDigits = contact.PhoneDigits ?? "02191093492",
            TelHref = contact.TelHref ?? "tel:02191093492",
            WhatsAppUrl = contact.WhatsAppUrl ?? contact.TelHref ?? "tel:02191093492",
            ContactUrl = Url.RouteUrl("contact-us") ?? "/contact-us/",
            BookingPopupUrl = "popup:booking",
            DoctorsUrl = "/doctors/",
            ServicesUrl = "/services/",
            DoctorImageUrl = doctorImage ?? "/template/assets/images/doctors/dr-hamedani.webp",
            IsPopup = string.Equals(Request.Query["embed"], "1", StringComparison.Ordinal)
        });
    }

    private async Task<bool> CanAccessAsync()
    {
        var blog = await _authorization.AuthorizeAsync(User, AuthPolicies.ViewBlog);
        if (blog.Succeeded)
            return true;

        var news = await _authorization.AuthorizeAsync(User, AuthPolicies.ViewNews);
        return news.Succeeded;
    }
}
