using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class WholesaleController : Controller
{
    private readonly IWholesaleService _wholesale;
    private readonly ICustomerGroupService _groups;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public WholesaleController(
        IWholesaleService wholesale,
        ICustomerGroupService groups,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _wholesale = wholesale;
        _groups = groups;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["WholesaleRequests"].Value;
        var result = await _wholesale.ListPagedAsync(new PagedRequest { Page = page }, cancellationToken);
        PageSlice.ApplyToViewBag(ViewBag, result);
        return View(result.Items);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var request = await _wholesale.GetAsync(id, cancellationToken);
        if (request is null)
            return NotFound();

        ViewData["Title"] = _localizer["WholesaleRequestDetail"].Value;
        var groups = await _groups.ListAsync(cancellationToken);
        return View(new WholesaleDetailViewModel
        {
            Request = request,
            AssignedGroupId = request.AssignedGroupId,
            AdminNotes = request.AdminNotes,
            GroupOptions = groups.Where(g => g.IsWholesale).Select(g => new SelectListItem(g.Name, g.Id.ToString(), g.Id == request.AssignedGroupId))
        });
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id, Guid? assignedGroupId, string? adminNotes, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _wholesale.ApproveAsync(id, new ReviewWholesaleRequestCommand(assignedGroupId, adminNotes), cancellationToken);
            TempData["Success"] = _localizer["WholesaleApproved"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Reject(Guid id, string? adminNotes, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _wholesale.RejectAsync(id, new ReviewWholesaleRequestCommand(null, adminNotes), cancellationToken);
            TempData["Success"] = _localizer["WholesaleRejected"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public string StatusLabel(WholesaleRequestStatus status) => status switch
    {
        WholesaleRequestStatus.Pending => _localizer["StatusPending"].Value,
        WholesaleRequestStatus.Approved => _localizer["StatusApproved"].Value,
        WholesaleRequestStatus.Rejected => _localizer["StatusRejected"].Value,
        _ => status.ToString()
    };
}
