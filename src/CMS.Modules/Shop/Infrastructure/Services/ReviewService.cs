using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Shop.Application.Interfaces;
using CMS.Modules.Shop.Application.Reviews;
using CMS.Modules.Shop.Domain.Entities;
using CMS.Modules.Shop.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Shop.Infrastructure.Services;

public sealed class ReviewService : IReviewService
{
    private readonly ShopDbContext _db;
    private readonly IValidator<SubmitReviewCommand> _submitValidator;

    public ReviewService(ShopDbContext db, IValidator<SubmitReviewCommand> submitValidator)
    {
        _db = db;
        _submitValidator = submitValidator;
    }

    public async Task<IReadOnlyList<ReviewListItemDto>> ListAsync(bool? approved = null, CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            new PagedRequest { Page = 1, PageSize = PagedRequest.MaxPageSize },
            approved,
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<ReviewListItemDto>> ListPagedAsync(
        PagedRequest request,
        bool? approved = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Reviews.AsNoTracking().AsQueryable();
        if (approved.HasValue)
            query = query.Where(r => r.IsApproved == approved.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip(request.Skip)
            .Take(request.NormalizedPageSize)
            .Select(r => new ReviewListItemDto(
                r.Id,
                r.ProductId,
                r.Product.Title,
                r.AuthorName,
                r.Rating,
                r.Comment,
                r.IsApproved,
                r.AdminResponse,
                r.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReviewListItemDto>(
            items,
            total,
            request.NormalizedPage,
            request.NormalizedPageSize);
    }

    public async Task<Guid> SubmitAsync(SubmitReviewCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _submitValidator.ValidateAsync(command, cancellationToken);
        if (!result.IsValid)
        {
            throw new DomainValidationException(result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        if (!await _db.Products.AnyAsync(p => p.Id == command.ProductId, cancellationToken))
            throw new NotFoundException(nameof(Product), command.ProductId);

        var review = ProductReview.Create(
            command.ProductId,
            command.UserId,
            command.AuthorName,
            command.Rating,
            command.Comment);

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);
        return review.Id;
    }

    public async Task ModerateAsync(Guid id, ModerateReviewCommand command, CancellationToken cancellationToken = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProductReview), id);

        if (command.Approve)
            review.Approve();
        else
            review.Reject();

        review.SetAdminResponse(command.AdminResponse);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
