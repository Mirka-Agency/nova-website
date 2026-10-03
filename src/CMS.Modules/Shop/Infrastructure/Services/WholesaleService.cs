using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Wholesale;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class WholesaleService : IWholesaleService
{
    private readonly ShopDbContext _db;
    private readonly ICustomerGroupService _customerGroups;
    private readonly IValidator<SubmitWholesaleRequestCommand> _submitValidator;

    public WholesaleService(
        ShopDbContext db,
        ICustomerGroupService customerGroups,
        IValidator<SubmitWholesaleRequestCommand> submitValidator)
    {
        _db = db;
        _customerGroups = customerGroups;
        _submitValidator = submitValidator;
    }

    public async Task<IReadOnlyList<WholesaleRequestListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new PagedRequest { Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<WholesaleRequestListItemDto>> ListPagedAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WholesaleRequests.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(r => new WholesaleRequestListItemDto(
                r.Id,
                r.CompanyName,
                r.ContactName,
                r.Email,
                r.Phone,
                r.Status,
                r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<WholesaleRequestListItemDto>(
            items,
            total,
            request.NormalizedPage,
            request.NormalizedPageSize);
    }

    public async Task<WholesaleRequestDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _db.WholesaleRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return request is null ? null : Map(request);
    }

    public async Task<Guid> SubmitAsync(SubmitWholesaleRequestCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _submitValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var request = WholesaleRequest.Create(
            command.UserId,
            command.CompanyName,
            command.BusinessInfo,
            command.ContactName,
            command.Phone,
            command.Email,
            command.Address,
            command.DocumentUrlsJson);

        _db.WholesaleRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return request.Id;
    }

    public async Task ApproveAsync(Guid id, ReviewWholesaleRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await _db.WholesaleRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(WholesaleRequest), id);

        if (!command.GroupId.HasValue)
            throw new DomainException("گروه مشتری برای تأیید الزامی است.");

        request.Approve(command.GroupId.Value, command.AdminNotes);

        if (!string.IsNullOrWhiteSpace(request.UserId))
            await _customerGroups.AssignUserAsync(request.UserId, command.GroupId.Value, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, ReviewWholesaleRequestCommand command, CancellationToken cancellationToken = default)
    {
        var request = await _db.WholesaleRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(WholesaleRequest), id);

        request.Reject(command.AdminNotes);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WholesaleRequestDetailDto Map(WholesaleRequest r) =>
        new(
            r.Id,
            r.UserId,
            r.CompanyName,
            r.BusinessInfo,
            r.ContactName,
            r.Phone,
            r.Email,
            r.Address,
            r.DocumentUrlsJson,
            r.Status,
            r.AdminNotes,
            r.AssignedGroupId,
            r.CreatedAtUtc,
            r.ReviewedAtUtc);
}
