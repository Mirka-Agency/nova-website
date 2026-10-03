using CMS.Application.Common.Features;
using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Popup.Application.Popups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Popup.Web.ViewComponents;

public sealed class PopupHostViewComponent : ViewComponent
{
    private readonly IPublicPopupQuery _popups;
    private readonly IFeatureManager _features;

    public PopupHostViewComponent(IPublicPopupQuery popups, IFeatureManager features)
    {
        _popups = popups;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Popup))
            return Content(string.Empty);

        var path = HttpContext.Request.Path.HasValue
            ? HttpContext.Request.Path.Value!
            : "/";

        var items = await _popups.GetForPathAsync(path, cancellationToken);
        if (items.Count == 0)
            return Content(string.Empty);

        return View(items);
    }
}
