using CMS.Application.Common.Paging;
using CMS.Modules.Shop.Application.Cart;
using CMS.Modules.Shop.Application.Catalog;
using CMS.Modules.Shop.Application.Categories;
using CMS.Modules.Shop.Application.Orders;
using CMS.Modules.Shop.Application.Pricing;
using CMS.Modules.Shop.Application.Products;
using CMS.Modules.Shop.Application.Promotions;
using CMS.Modules.Shop.Application.Reports;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Application.Settings;
using CMS.Modules.Shop.Application.Shipping;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Domain.Enums;

namespace CMS.Modules.Shop.Application.Interfaces;

public interface IShopCategoryService
{
    Task<IReadOnlyList<ShopCategoryListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<ShopCategoryDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveShopCategoryCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveShopCategoryCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IProductService
{
    Task<IReadOnlyList<ProductListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> ListPagedAsync(ProductListRequest request, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveProductCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveProductCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddVariationAsync(Guid productId, SaveProductVariationCommand command, CancellationToken cancellationToken = default);
    Task UpdateVariationAsync(Guid productId, Guid variationId, SaveProductVariationCommand command, CancellationToken cancellationToken = default);
    Task DeleteVariationAsync(Guid productId, Guid variationId, CancellationToken cancellationToken = default);
    Task ReplaceSpecificationsAsync(
        Guid productId,
        IReadOnlyList<SaveProductSpecificationCommand> specifications,
        CancellationToken cancellationToken = default);
}

public interface IProductImportService
{
    byte[] GetSampleCsv();
    Task<ProductImportResult> ImportAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

public interface IPublicProductQuery
{
    Task<IReadOnlyList<PublicProductListItemDto>> ListPublishedAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<PublicProductListItemDto>> ListPublishedPagedAsync(
        int page,
        int pageSize,
        string? userId = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
    Task<PublicProductDetailDto?> GetPublishedBySlugAsync(string slug, string? userId = null, CancellationToken cancellationToken = default);
    Task<PublicCategoryDetailDto?> GetPublishedCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IShopSettingsService
{
    Task<ShopSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateModeAsync(UpdateShopModeCommand command, CancellationToken cancellationToken = default);
    Task UpdateCommerceAsync(UpdateShopCommerceSettingsCommand command, CancellationToken cancellationToken = default);
    Task UpdateSellerAsync(UpdateShopSellerSettingsCommand command, CancellationToken cancellationToken = default);
}

public interface ICartService
{
    Task<CartDto> GetAsync(CancellationToken cancellationToken = default);
    Task<int> GetItemCountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AddToCartCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateCartItemCommand command, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid productId, Guid? variationId = null, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task ApplyCouponAsync(ApplyCouponCommand command, CancellationToken cancellationToken = default);
    Task SetShippingAsync(SetCartShippingCommand command, CancellationToken cancellationToken = default);
    Task<decimal> GetWeightAsync(CancellationToken cancellationToken = default);
}

public interface IOrderService
{
    Task<IReadOnlyList<OrderListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<OrderListItemDto>> ListPagedAsync(
        PagedRequest request,
        IReadOnlyCollection<OrderStatus>? statuses = null,
        CancellationToken cancellationToken = default);
    Task<PagedResult<OrderListItemDto>> ListPagedAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusesAsync(
        IReadOnlyCollection<OrderStatus> statuses,
        CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderListItemDto>> ListForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<OrderDetailDto?> GetForUserAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(Guid id, ChangeOrderStatusCommand command, CancellationToken cancellationToken = default);
    Task UpdateAdminNotesAsync(Guid id, UpdateOrderAdminNotesCommand command, CancellationToken cancellationToken = default);
    Task ConfirmBankTransferPaidAsync(Guid id, ConfirmBankTransferPaidCommand command, CancellationToken cancellationToken = default);
    Task CancelForUserAsync(string userId, Guid id, CancellationToken cancellationToken = default);
    Task ProcessPendingPaymentMaintenanceAsync(CancellationToken cancellationToken = default);
    Task<CheckoutResultDto> CheckoutAsync(CheckoutCommand command, CancellationToken cancellationToken = default);
}

public interface IBrandService
{
    Task<IReadOnlyList<BrandListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<BrandDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveBrandCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveBrandCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAttributeService
{
    Task<IReadOnlyList<AttributeListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AttributeListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveAttributeCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveAttributeCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ICustomerGroupService
{
    Task<IReadOnlyList<CustomerGroupDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveCustomerGroupCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveCustomerGroupCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignUserAsync(string userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<CustomerGroupDto?> GetUserGroupAsync(string userId, CancellationToken cancellationToken = default);
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
}

public interface IPriceRuleService
{
    Task<IReadOnlyList<PriceRuleDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SavePriceRuleCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SavePriceRuleCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IPricingEngine
{
    Task<PriceQuoteResult> QuoteAsync(PriceQuoteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<(Guid ProductId, Guid? VariationId), PriceQuoteResult>> QuoteManyAsync(
        IReadOnlyList<PriceQuoteRequest> requests,
        CancellationToken cancellationToken = default);
    Task<Guid?> ResolveCustomerGroupIdAsync(string? userId, CancellationToken cancellationToken = default);
}

public interface IWholesaleService
{
    Task<IReadOnlyList<WholesaleRequestListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<WholesaleRequestListItemDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<WholesaleRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> SubmitAsync(SubmitWholesaleRequestCommand command, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid id, ReviewWholesaleRequestCommand command, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid id, ReviewWholesaleRequestCommand command, CancellationToken cancellationToken = default);
}

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<CouponDto>> ListPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveCouponCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveCouponCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IShippingService
{
    Task<IReadOnlyList<ShippingMethodDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(SaveShippingMethodCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, SaveShippingMethodCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShippingQuoteResult?> QuoteAsync(ShippingQuoteRequest request, CancellationToken cancellationToken = default);
}

public interface IReviewService
{
    Task<IReadOnlyList<ReviewListItemDto>> ListAsync(bool? approved = null, CancellationToken cancellationToken = default);
    Task<PagedResult<ReviewListItemDto>> ListPagedAsync(PagedRequest request, bool? approved = null, CancellationToken cancellationToken = default);
    Task<Guid> SubmitAsync(SubmitReviewCommand command, CancellationToken cancellationToken = default);
    Task ModerateAsync(Guid id, ModerateReviewCommand command, CancellationToken cancellationToken = default);
}

public interface ICommerceReportService
{
    Task<CommerceReportDto> GetAsync(CancellationToken cancellationToken = default);
}
