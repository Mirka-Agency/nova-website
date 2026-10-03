using System.Globalization;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Application.Common.Paging;
using CMS.Application.Users;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Promotions;
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
public class CouponsController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();

    private readonly ICouponService _coupons;
    private readonly IUserAccountLookup _users;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public CouponsController(
        ICouponService coupons,
        IUserAccountLookup users,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _coupons = coupons;
        _users = users;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["Coupons"].Value;
        var result = await _coupons.ListPagedAsync(new PagedRequest { Page = page }, cancellationToken);
        PageSlice.ApplyToViewBag(ViewBag, result);
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        ViewData["Title"] = _localizer["CreateCoupon"].Value;
        return View(BuildForm(new CouponFormViewModel()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CouponFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model = BuildForm(model);
        ViewData["Title"] = _localizer["CreateCoupon"].Value;
        ValidateDates(model);
        var allowedUserId = await ResolveAllowedUserIdAsync(model, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _coupons.CreateAsync(ToCommand(model, allowedUserId), cancellationToken);
            TempData["Success"] = _localizer["CouponCreated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var coupons = await _coupons.ListAsync(cancellationToken);
        var coupon = coupons.FirstOrDefault(c => c.Id == id);
        if (coupon is null)
            return NotFound();

        string? allowedUserEmail = null;
        if (!string.IsNullOrWhiteSpace(coupon.AllowedUserId))
            allowedUserEmail = await _users.GetEmailAsync(coupon.AllowedUserId, cancellationToken);

        ViewData["Title"] = _localizer["EditCoupon"].Value;
        return View(BuildForm(new CouponFormViewModel
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Name = coupon.Name,
            DiscountType = coupon.DiscountType is DiscountType.Percentage or DiscountType.FixedAmount
                ? coupon.DiscountType
                : DiscountType.Percentage,
            Amount = coupon.Amount,
            MinOrderAmount = coupon.MinOrderAmount,
            MaxRedemptions = coupon.MaxRedemptions,
            AllowedUserEmail = allowedUserEmail,
            StartsAtLocal = ToIranLocal(coupon.StartsAtUtc),
            EndsAtLocal = ToIranLocal(coupon.EndsAtUtc),
            IsActive = coupon.IsActive
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CouponFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        model.Id = id;
        model = BuildForm(model);
        ViewData["Title"] = _localizer["EditCoupon"].Value;
        ValidateDates(model);
        var allowedUserId = await ResolveAllowedUserIdAsync(model, cancellationToken);
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _coupons.UpdateAsync(id, ToCommand(model, allowedUserId), cancellationToken);
            TempData["Success"] = _localizer["CouponUpdated"].Value;
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _coupons.DeleteAsync(id, cancellationToken);
            TempData["Success"] = _localizer["CouponDeleted"].Value;
        }
        catch (NotFoundException)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private CouponFormViewModel BuildForm(CouponFormViewModel model)
    {
        model.DiscountTypeOptions =
        [
            new SelectListItem(_localizer["DiscountTypePercentage"].Value, nameof(DiscountType.Percentage),
                model.DiscountType == DiscountType.Percentage),
            new SelectListItem(_localizer["DiscountTypeFixedAmount"].Value, nameof(DiscountType.FixedAmount),
                model.DiscountType == DiscountType.FixedAmount)
        ];
        return model;
    }

    private async Task<string?> ResolveAllowedUserIdAsync(
        CouponFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.AllowedUserEmail))
            return null;

        var userId = await _users.FindIdByEmailAsync(model.AllowedUserEmail.Trim(), cancellationToken);
        if (userId is null)
            ModelState.AddModelError(nameof(model.AllowedUserEmail), _localizer["CouponUserNotFound"].Value);

        return userId;
    }

    private void ValidateDates(CouponFormViewModel model)
    {
        if (model.DiscountType == DiscountType.Percentage && model.Amount > 100)
            ModelState.AddModelError(nameof(model.Amount), "برای تخفیف درصدی، مقدار باید حداکثر ۱۰۰ باشد.");

        DateTime? starts = null;
        DateTime? ends = null;

        if (!string.IsNullOrWhiteSpace(model.StartsAtLocal))
        {
            if (!TryParseJalaliDateTime(model.StartsAtLocal, out var parsedStarts))
                ModelState.AddModelError(nameof(model.StartsAtLocal), "تاریخ شروع نامعتبر است.");
            else
                starts = parsedStarts;
        }

        if (!string.IsNullOrWhiteSpace(model.EndsAtLocal))
        {
            if (!TryParseJalaliDateTime(model.EndsAtLocal, out var parsedEnds))
                ModelState.AddModelError(nameof(model.EndsAtLocal), "تاریخ پایان نامعتبر است.");
            else
                ends = parsedEnds;
        }

        if (starts.HasValue && ends.HasValue && ends.Value < starts.Value)
            ModelState.AddModelError(nameof(model.EndsAtLocal), "تاریخ پایان باید بعد از تاریخ شروع باشد.");
    }

    private static SaveCouponCommand ToCommand(CouponFormViewModel model, string? allowedUserId) =>
        new(
            model.Code,
            model.Name,
            model.DiscountType,
            model.Amount,
            null,
            null,
            null,
            model.MinOrderAmount,
            ToUtc(model.StartsAtLocal),
            ToUtc(model.EndsAtLocal),
            model.MaxRedemptions,
            model.IsActive,
            allowedUserId);

    private static string? ToIranLocal(DateTime? utc)
    {
        if (!utc.HasValue)
            return null;

        var value = DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(value, IranTimeZone);
        var persian = new PersianCalendar();
        return string.Create(CultureInfo.InvariantCulture,
            $"{persian.GetYear(local):0000}/{persian.GetMonth(local):00}/{persian.GetDayOfMonth(local):00} {local.Hour:00}:{local.Minute:00}");
    }

    private static DateTime? ToUtc(string? jalaliLocal)
    {
        if (string.IsNullOrWhiteSpace(jalaliLocal))
            return null;

        if (!TryParseJalaliDateTime(jalaliLocal, out var local))
            return null;

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

    private static bool TryParseJalaliDateTime(string value, out DateTime local)
    {
        local = default;
        var normalized = NormalizeDigits(value.Trim());
        var match = Regex.Match(
            normalized,
            @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$");
        if (!match.Success)
            return false;

        var year = int.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        var day = int.Parse(match.Groups["d"].Value, CultureInfo.InvariantCulture);
        var hour = match.Groups["h"].Success
            ? int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture)
            : 0;
        var minute = match.Groups["min"].Success
            ? int.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture)
            : 0;
        var second = match.Groups["s"].Success
            ? int.Parse(match.Groups["s"].Value, CultureInfo.InvariantCulture)
            : 0;

        try
        {
            local = new PersianCalendar().ToDateTime(year, month, day, hour, minute, second, 0);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string NormalizeDigits(string value)
    {
        var buffer = value.ToCharArray();
        for (var i = 0; i < buffer.Length; i++)
        {
            var c = buffer[i];
            if (c is >= '\u06F0' and <= '\u06F9')
                buffer[i] = (char)('0' + (c - '\u06F0'));
            else if (c is >= '\u0660' and <= '\u0669')
                buffer[i] = (char)('0' + (c - '\u0660'));
        }

        return new string(buffer);
    }

    private static TimeZoneInfo ResolveIranTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                "Asia/Tehran",
                TimeSpan.FromHours(3.5),
                "Iran Standard Time",
                "Iran Standard Time");
        }
    }

    private void AddErrors(ValidationException ex)
    {
        foreach (var (key, messages) in ex.Errors)
            foreach (var message in messages)
                ModelState.AddModelError(key, message);
    }
}
