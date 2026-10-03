using CMS.Application.Audit;
using CMS.Application.Common.Paging;
using CMS.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CMS.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.AdminOnly)]
public class AuditController : Controller
{
    private readonly IAuditQueryService _audit;
    private readonly IStringLocalizer<AdminShared> _localizer;

    public AuditController(IAuditQueryService audit, IStringLocalizer<AdminShared> localizer)
    {
        _audit = audit;
        _localizer = localizer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = _localizer["AuditLog"].Value;
        var result = await _audit.ListPagedAsync(new PagedRequest { Page = page }, cancellationToken);
        PageSlice.ApplyToViewBag(ViewBag, result);
        return View(result.Items);
    }
}
