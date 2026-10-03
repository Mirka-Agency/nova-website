using System.Globalization;
using System.Text.RegularExpressions;
using CMS.Application.Common.Features;
using CMS.Application.Settings;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Invoices;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Domain.Enums;
using CMS.Modules.Shop.Web.Areas.Admin;
using CMS.Modules.Shop.Web.Areas.Admin.ViewModels;
using CMS.Modules.Shop.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.FeatureManagement;

namespace CMS.Modules.Shop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "ViewShop")]
public class OrdersController : Controller
{
    private static readonly TimeZoneInfo IranTimeZone = ResolveIranTimeZone();
    private static readonly Regex JalaliDateRegex = new(
        @"^(?<y>\d{4})/(?<m>\d{1,2})/(?<d>\d{1,2})(?:\s+(?<h>\d{1,2}):(?<min>\d{1,2})(?::(?<s>\d{1,2}))?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IOrderService _orders;
    private readonly IShopSettingsService _shopSettings;
    private readonly ISiteSettingsService _siteSettings;
    private readonly IFeatureManager _features;
    private readonly IStringLocalizer<ShopAdmin> _localizer;

    public OrdersController(
        IOrderService orders,
        IShopSettingsService shopSettings,
        ISiteSettingsService siteSettings,
        IFeatureManager features,
        IStringLocalizer<ShopAdmin> localizer)
    {
        _orders = orders;
        _shopSettings = shopSettings;
        _siteSettings = siteSettings;
        _features = features;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        string? q = null,
        string? status = null,
        PaymentStatus? paymentStatus = null,
        PaymentMethod? paymentMethod = null,
        bool? isWholesale = null,
        string? from = null,
        string? to = null,
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var filter = ResolveStatusFilter(status);
        var fromLocal = NormalizeDateInput(from);
        var toLocal = NormalizeDateInput(to);

        var result = await _orders.ListPagedAsync(
            new OrderListRequest
            {
                Page = page,
                Search = q,
                Statuses = filter.Statuses,
                PaymentStatus = paymentStatus,
                PaymentMethod = paymentMethod,
                IsWholesale = isWholesale,
                FromUtc = ToUtc(fromLocal, endOfDay: false),
                ToUtc = ToUtc(toLocal, endOfDay: true),
                Sort = sort
            },
            cancellationToken);

        var model = new OrderIndexViewModel
        {
            Search = q,
            Status = filter.Key,
            PaymentStatus = paymentStatus,
            PaymentMethod = paymentMethod,
            IsWholesale = isWholesale,
            FromLocal = fromLocal,
            ToLocal = toLocal,
            Sort = sort,
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount,
            PageTitle = filter.Title,
            Items = result.Items.Select(o => new OrderListItemViewModel
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                StatusDisplay = StatusLabel(o.Status),
                TotalDisplay = $"{o.TotalAmount:N0} {o.Currency}",
                CreatedAtUtc = o.CreatedAtUtc,
                Status = o.Status,
                PaymentExpiresAtUtc = o.PaymentExpiresAtUtc
            }).ToList()
        };

        ViewData["Title"] = filter.Title;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var order = await _orders.GetAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        ViewData["Title"] = _localizer["OrderDetail"].Value;
        return View(MapDetail(order));
    }

    public async Task<IActionResult> PrintShipment(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var order = await _orders.GetAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        var settings = await _shopSettings.GetAsync(cancellationToken);
        var siteSettings = await _siteSettings.GetAsync(cancellationToken);
        var (recipientAddress, recipientPostal) = ShipmentPrintHelper.SplitPostalCode(order.ShippingAddress);
        var model = new OrderShipmentPrintViewModel
        {
            LogoUrl = siteSettings.LogoUrl,
            SiteName = siteSettings.SiteName,
            SellerName = settings.SellerName ?? siteSettings.SiteName,
            SellerPhone = settings.SellerPhone ?? siteSettings.ContactPhone,
            SellerProvince = settings.SellerProvince,
            SellerCity = settings.SellerCity,
            SellerAddress = settings.SellerAddress ?? siteSettings.Address,
            SellerPostalCode = settings.SellerPostalCode,
            RecipientName = order.RecipientName ?? order.CustomerName,
            RecipientPhone = order.RecipientPhone ?? order.CustomerPhone,
            ShippingCity = order.ShippingCity,
            ShippingAddress = recipientAddress,
            RecipientPostalCode = recipientPostal
        };

        return View(model);
    }

    public async Task<IActionResult> PrintInvoice(Guid id, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        var order = await _orders.GetAsync(id, cancellationToken);
        if (order is null)
            return NotFound();

        var shopSettings = await _shopSettings.GetAsync(cancellationToken);
        var siteSettings = await _siteSettings.GetAsync(cancellationToken);
        var sellerName = shopSettings.SellerName ?? siteSettings.SiteName;
        var sellerPhone = shopSettings.SellerPhone ?? siteSettings.ContactPhone;
        var sellerEmail = shopSettings.SellerEmail ?? siteSettings.ContactEmail;
        var sellerAddress = shopSettings.SellerAddress ?? siteSettings.Address;
        var sellerFields = InvoicePrintFieldBuilder.BuildSellerFields(
            shopSettings.InvoiceSellerDisplayFields,
            sellerName,
            shopSettings.SellerNationalId,
            shopSettings.SellerEconomicCode,
            shopSettings.SellerRegistrationNumber,
            sellerPhone,
            sellerEmail,
            shopSettings.SellerProvince,
            shopSettings.SellerCity,
            shopSettings.SellerPostalCode,
            sellerAddress);
        var buyerFields = InvoicePrintFieldBuilder.BuildBuyerFields(
            shopSettings.InvoiceBuyerDisplayFields,
            order.CustomerName,
            order.CustomerEmail,
            order.CustomerPhone,
            order.RecipientName,
            order.RecipientPhone,
            order.ShippingCity,
            order.ShippingAddress,
            order.IsWholesale);
        var model = new OrderInvoicePrintViewModel
        {
            OrderNumber = order.OrderNumber,
            CreatedAtUtc = order.CreatedAtUtc,
            Currency = order.Currency,
            LogoUrl = siteSettings.LogoUrl,
            SiteName = siteSettings.SiteName,
            StatusDisplay = StatusLabel(order.Status),
            PaymentStatusDisplay = PaymentStatusLabel(order.PaymentStatus),
            PaymentMethodDisplay = order.PaymentMethod.HasValue
                ? PaymentMethodLabel(order.PaymentMethod.Value)
                : null,
            PaymentTransactionId = order.PaymentTransactionId,
            PaymentProvider = order.PaymentProvider,
            ShippingMethodName = order.ShippingMethodName,
            CouponCode = order.CouponCode,
            Notes = order.Notes,
            IsWholesale = order.IsWholesale,
            SellerName = sellerName,
            CustomerName = order.CustomerName,
            SubtotalDisplay = Money(order.SubtotalAmount, order.Currency),
            ShippingAmountDisplay = Money(order.ShippingAmount, order.Currency),
            DiscountAmountDisplay = Money(order.DiscountAmount, order.Currency),
            VatAmountDisplay = order.VatAmount > 0 ? Money(order.VatAmount, order.Currency) : null,
            VatPercent = order.VatPercent,
            TotalDisplay = Money(order.TotalAmount, order.Currency),
            SubtotalAmount = order.SubtotalAmount,
            ShippingAmount = order.ShippingAmount,
            DiscountAmount = order.DiscountAmount,
            VatAmount = order.VatAmount,
            TotalAmount = order.TotalAmount,
            SellerFields = sellerFields.Select(f => new InvoicePartyFieldViewModel
            {
                Label = _localizer[f.LabelKey].Value,
                Value = f.LabelKey == "IsWholesale" ? _localizer["Yes"].Value : f.Value,
                IsLtr = f.IsLtr
            }).ToList(),
            BuyerFields = buyerFields.Select(f => new InvoicePartyFieldViewModel
            {
                Label = _localizer[f.LabelKey].Value,
                Value = f.LabelKey == "IsWholesale" ? _localizer["Yes"].Value : f.Value,
                IsLtr = f.IsLtr
            }).ToList(),
            Lines = order.Lines.Select((l, index) => new OrderInvoiceLineViewModel
            {
                RowNumber = index + 1,
                ProductTitle = l.ProductTitle,
                Sku = l.Sku,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineTotal = l.LineTotal,
                UnitPriceDisplay = Money(l.UnitPrice, order.Currency),
                LineTotalDisplay = Money(l.LineTotal, order.Currency)
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(Guid id, OrderStatus status, CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _orders.ChangeStatusAsync(id, new ChangeOrderStatusCommand(status), cancellationToken);
            TempData["Success"] = _localizer["OrderStatusUpdated"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (ValidationException ex)
        {
            foreach (var (_, messages) in ex.Errors)
                foreach (var message in messages)
                    TempData["Error"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [Authorize(Policy = "ManageShop")]
    public async Task<IActionResult> ConfirmBankTransfer(
        Guid id,
        string? transactionReference,
        CancellationToken cancellationToken)
    {
        if (!await _features.IsEnabledAsync(FeatureNames.Shop))
            return NotFound();

        try
        {
            await _orders.ConfirmBankTransferPaidAsync(
                id,
                new ConfirmBankTransferPaidCommand(transactionReference),
                cancellationToken);
            TempData["Success"] = _localizer["BankTransferConfirmed"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private OrderDetailViewModel MapDetail(OrderDetailDto order) =>
        new()
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            StatusDisplay = StatusLabel(order.Status),
            PaymentStatus = order.PaymentStatus,
            PaymentStatusDisplay = PaymentStatusLabel(order.PaymentStatus),
            PaymentMethod = order.PaymentMethod,
            PaymentMethodDisplay = order.PaymentMethod.HasValue
                ? PaymentMethodLabel(order.PaymentMethod.Value)
                : null,
            CanConfirmBankTransfer = order.PaymentMethod == PaymentMethod.BankTransfer
                && order.PaymentStatus == PaymentStatus.Unpaid
                && order.Status != OrderStatus.Cancelled,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            ShippingAddress = order.ShippingAddress,
            ShippingCity = order.ShippingCity,
            ShippingMethodName = order.ShippingMethodName,
            ShippingEstimatedDelivery = order.ShippingEstimatedDelivery,
            SubtotalDisplay = Money(order.SubtotalAmount, order.Currency),
            ShippingAmountDisplay = Money(order.ShippingAmount, order.Currency),
            DiscountAmountDisplay = Money(order.DiscountAmount, order.Currency),
            VatAmountDisplay = order.VatAmount > 0 ? Money(order.VatAmount, order.Currency) : null,
            VatPercent = order.VatPercent,
            CouponCode = order.CouponCode,
            TotalDisplay = Money(order.TotalAmount, order.Currency),
            PaymentProvider = order.PaymentProvider,
            PaymentTransactionId = order.PaymentTransactionId,
            Notes = order.Notes,
            AdminNotes = order.AdminNotes,
            IsWholesale = order.IsWholesale,
            CreatedAtUtc = order.CreatedAtUtc,
            PaymentExpiresAtUtc = order.PaymentExpiresAtUtc,
            Lines = order.Lines.Select(l => new OrderLineViewModel
            {
                ProductTitle = l.ProductTitle,
                Sku = l.Sku,
                UnitPriceDisplay = Money(l.UnitPrice, order.Currency),
                Quantity = l.Quantity,
                LineTotalDisplay = Money(l.LineTotal, order.Currency)
            }).ToList(),
            StatusOptions = Enum.GetValues<OrderStatus>()
                .Select(s => new SelectListItem(StatusLabel(s), s.ToString(), s == order.Status))
        };

    private static string Money(decimal amount, string currency) => $"{amount:N0} {currency}";

    private (string Key, string Title, IReadOnlyCollection<OrderStatus>? Statuses) ResolveStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return ("", _localizer["Orders"].Value, null);

        var key = status.Trim();
        var lower = key.ToLowerInvariant();
        return lower switch
        {
            "pending" => ("pending", _localizer["OrdersPending"].Value, [OrderStatus.Pending]),
            "processing" => (
                "processing",
                _localizer["OrdersProcessing"].Value,
                [
                    OrderStatus.Reviewing,
                    OrderStatus.Approved,
                    OrderStatus.Processing,
                    OrderStatus.ReadyForShipping
                ]),
            "shipped" => ("shipped", _localizer["OrdersShipped"].Value, [OrderStatus.Shipped]),
            "completed" => ("completed", _localizer["OrdersCompleted"].Value, [OrderStatus.Completed]),
            "cancelled" => ("cancelled", _localizer["OrdersCancelled"].Value, [OrderStatus.Cancelled]),
            _ when Enum.TryParse<OrderStatus>(key, ignoreCase: true, out var exact) =>
                (exact.ToString(), StatusLabel(exact), [exact]),
            _ => ("", _localizer["Orders"].Value, null)
        };
    }

    private string StatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Pending => _localizer["StatusPendingPayment"].Value,
        OrderStatus.Reviewing => _localizer["StatusReviewing"].Value,
        OrderStatus.Approved => _localizer["StatusApproved"].Value,
        OrderStatus.Processing => _localizer["StatusProcessing"].Value,
        OrderStatus.ReadyForShipping => _localizer["StatusReadyForShipping"].Value,
        OrderStatus.Shipped => _localizer["StatusShipped"].Value,
        OrderStatus.Completed => _localizer["StatusCompleted"].Value,
        OrderStatus.Cancelled => _localizer["StatusCancelled"].Value,
        _ => status.ToString()
    };

    private string PaymentStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => _localizer["PaymentStatusUnpaid"].Value,
        PaymentStatus.Paid => _localizer["PaymentStatusPaid"].Value,
        PaymentStatus.InvoiceRequested => _localizer["PaymentStatusInvoiceRequested"].Value,
        _ => status.ToString()
    };

    private string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Online => _localizer["PaymentMethodOnline"].Value,
        PaymentMethod.BankTransfer => _localizer["PaymentMethodBankTransfer"].Value,
        PaymentMethod.CashOnDelivery => _localizer["PaymentMethodCashOnDelivery"].Value,
        PaymentMethod.Invoice => _localizer["PaymentMethodInvoice"].Value,
        PaymentMethod.PayLater => _localizer["PaymentMethodPayLater"].Value,
        PaymentMethod.SnapPay => _localizer["PaymentMethodSnapPay"].Value,
        _ => method.ToString()
    };

    private static string? NormalizeDateInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = NormalizeDigits(value.Trim()).Replace('-', '/');
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static DateTime? ToUtc(string? localDate, bool endOfDay)
    {
        if (string.IsNullOrWhiteSpace(localDate))
            return null;

        if (!TryParseLocalDate(localDate, endOfDay, out var local))
            return null;

        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, IranTimeZone);
    }

    private static bool TryParseLocalDate(string value, bool endOfDay, out DateTime local)
    {
        local = default;

        if (TryParseJalaliDateTime(value, out local))
        {
            if (endOfDay && !value.Contains(':'))
                local = local.Date.AddDays(1).AddTicks(-1);
            return true;
        }

        if (DateTime.TryParseExact(
                value.Replace('/', '-'),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var gregorian))
        {
            local = endOfDay ? gregorian.Date.AddDays(1).AddTicks(-1) : gregorian.Date;
            return true;
        }

        return false;
    }

    private static bool TryParseJalaliDateTime(string value, out DateTime local)
    {
        local = default;
        var match = JalaliDateRegex.Match(value);
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

        if (year is < 1200 or > 1600)
            return false;

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
}
