using CMS.Application.Common.Features;
using CMS.Modules.Shop.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.ViewComponents;

public sealed class CartNavViewComponent : ViewComponent
{
    private readonly ICartService _cart;
    private readonly IFeatureManager _features;

    public CartNavViewComponent(ICartService cart, IFeatureManager features)
    {
        _cart = cart;
        _features = features;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return Content(string.Empty);

        var count = await _cart.GetItemCountAsync(cancellationToken);
        return View(count);
    }
}
