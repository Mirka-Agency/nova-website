using System.ComponentModel.DataAnnotations;
using CMS.Modules.Shop.Application.Account;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Payments;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CMS.Modules.Shop.Web.ViewModels;

public sealed class CartPageViewModel
{
    public CartDto Cart { get; init; } = new([], "IRR", 0, 0, 0, 0, null, 0, null, null, null);
    public bool EcommerceEnabled { get; init; }
    public string? CouponCode { get; set; }
}

public sealed class CheckoutFormViewModel
{
    public CartDto Cart { get; set; } = new([], "IRR", 0, 0, 0, 0, null, 0, null, null, null);

    public Guid? AddressId { get; set; }

    public Guid? PaymentProviderConfigId { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;
    public Guid? ShippingMethodId { get; set; }

    [StringLength(2000, ErrorMessage = "یادداشت خیلی طولانی است.")]
    public string? Notes { get; set; }

    public bool ProfileComplete { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfilePhone { get; set; }
    public string? ProfileEmail { get; set; }

    public IReadOnlyList<CustomerAddressDto> Addresses { get; set; } = [];
    public IEnumerable<SelectListItem> ShippingMethodOptions { get; set; } = [];
    public IEnumerable<SelectListItem> PaymentMethodOptions { get; set; } = [];
    public IReadOnlyList<PaymentProviderConfigDto> OnlinePaymentProviders { get; set; } = [];
    public bool SnapPayEnabled { get; set; }
    public IReadOnlyList<ShippingMethodDto> ShippingMethods { get; set; } = [];
    public IReadOnlyDictionary<Guid, decimal> ShippingCosts { get; set; } = new Dictionary<Guid, decimal>();
}

public sealed class CheckoutResultViewModel
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public bool PaymentSucceeded { get; init; }
    public bool AwaitingOfflinePayment { get; init; }
    public string? PaymentMessage { get; init; }
    public string? TransactionId { get; init; }
    public CMS.Modules.Shop.Application.Settings.BankTransferDetailsDto? BankTransfer { get; init; }
}

public sealed class SubmitReviewFormViewModel
{
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "نام الزامی است.")]
    [StringLength(200)]
    public string AuthorName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "نظر الزامی است.")]
    [StringLength(4000)]
    public string Comment { get; set; } = string.Empty;
}

public sealed class WholesaleApplyFormViewModel
{
    [Required(ErrorMessage = "نام شرکت الزامی است.")]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? BusinessInfo { get; set; }

    [Required(ErrorMessage = "نام تماس الزامی است.")]
    [StringLength(200)]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "تلفن الزامی است.")]
    [StringLength(40)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "آدرس الزامی است.")]
    [StringLength(1000)]
    public string Address { get; set; } = string.Empty;
}
