using CMS.Application.Common.Paging;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.AntiSpam;
using CMS.Modules.Comments.Application.Comments;
using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Domain.Entities;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DomainValidationException = CMS.Domain.Exceptions.ValidationException;

namespace CMS.Modules.Comments.Infrastructure.Services;

public sealed class CommentService : ICommentService
{
    private readonly CommentsDbContext _db;
    private readonly ICommentSettingsService _settings;
    private readonly ICommentCaptchaValidator _captcha;
    private readonly IValidator<SubmitCommentCommand> _submitValidator;
    private readonly IValidator<UpdateCommentCommand> _updateValidator;

    public CommentService(
        CommentsDbContext db,
        ICommentSettingsService settings,
        ICommentCaptchaValidator captcha,
        IValidator<SubmitCommentCommand> submitValidator,
        IValidator<UpdateCommentCommand> updateValidator)
    {
        _db = db;
        _settings = settings;
        _captcha = captcha;
        _submitValidator = submitValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<CommentListItemDto>> ListAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = await ListPagedAsync(
            query with { Page = 1, PageSize = PagedRequest.MaxPageSize },
            cancellationToken);
        return page.Items;
    }

    public async Task<PagedResult<CommentListItemDto>> ListPagedAsync(
        CommentListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.Comments.AsNoTracking().AsQueryable();

        if (query.TargetType.HasValue)
            q = q.Where(c => c.TargetType == query.TargetType.Value);
        if (query.Status.HasValue)
            q = q.Where(c => c.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(c =>
                c.AuthorName.Contains(term)
                || c.Body.Contains(term)
                || c.TargetTitle.Contains(term)
                || (c.AuthorEmail != null && c.AuthorEmail.Contains(term))
                || (c.AuthorPhone != null && c.AuthorPhone.Contains(term)));
        }

        if (query.FromUtc.HasValue)
            q = q.Where(c => c.PublishedAtUtc >= query.FromUtc.Value);

        if (query.ToUtc.HasValue)
            q = q.Where(c => c.PublishedAtUtc <= query.ToUtc.Value);

        var total = await q.CountAsync(cancellationToken);
        var items = await ApplySort(q, query.Sort)
            .Skip(query.Skip)
            .Take(query.NormalizedPageSize)
            .Select(c => new CommentListItemDto(
                c.Id,
                c.TargetType,
                c.TargetId,
                c.TargetTitle,
                c.AuthorName,
                c.AuthorEmail,
                c.AuthorPhone,
                c.Body,
                c.Status,
                c.PublishedAtUtc,
                c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentListItemDto>(
            items,
            total,
            query.NormalizedPage,
            query.NormalizedPageSize);
    }

    private static IQueryable<Comment> ApplySort(IQueryable<Comment> query, string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase)
            ? query.OrderBy(c => c.CreatedAtUtc)
            : string.Equals(sort, "published", StringComparison.OrdinalIgnoreCase)
                ? query.OrderByDescending(c => c.PublishedAtUtc)
                : string.Equals(sort, "published_asc", StringComparison.OrdinalIgnoreCase)
                    ? query.OrderBy(c => c.PublishedAtUtc)
                    : string.Equals(sort, "author", StringComparison.OrdinalIgnoreCase)
                        ? query.OrderBy(c => c.AuthorName)
                        : string.Equals(sort, "author_desc", StringComparison.OrdinalIgnoreCase)
                            ? query.OrderByDescending(c => c.AuthorName)
                            : query.OrderByDescending(c => c.CreatedAtUtc);

    public async Task<CommentListItemDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Comments.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CommentListItemDto(
                c.Id,
                c.TargetType,
                c.TargetId,
                c.TargetTitle,
                c.AuthorName,
                c.AuthorEmail,
                c.AuthorPhone,
                c.Body,
                c.Status,
                c.PublishedAtUtc,
                c.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PublicCommentDto>> ListApprovedForTargetAsync(
        CommentTargetType targetType,
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Comments.AsNoTracking()
            .Where(c =>
                c.TargetType == targetType
                && c.TargetId == targetId
                && c.Status == CommentStatus.Approved)
            .OrderByDescending(c => c.PublishedAtUtc)
            .Select(c => new PublicCommentDto(c.Id, c.AuthorName, c.Body, c.PublishedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> SubmitAsync(SubmitCommentCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _submitValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var settings = await _settings.GetAsync(cancellationToken);
        if (!settings.IsEnabledFor(command.TargetType))
            throw new DomainException("ثبت کامنت برای این بخش غیرفعال است.");

        if (!settings.AllowAnonymous && string.IsNullOrWhiteSpace(command.UserId))
            throw new DomainException("برای ثبت کامنت باید وارد حساب کاربری شوید.");

        var fieldErrors = new Dictionary<string, string[]>();
        if (settings.RequireEmail && string.IsNullOrWhiteSpace(command.AuthorEmail))
            fieldErrors[nameof(command.AuthorEmail)] = ["ایمیل الزامی است."];
        if (settings.RequirePhone && string.IsNullOrWhiteSpace(command.AuthorPhone))
            fieldErrors[nameof(command.AuthorPhone)] = ["تلفن الزامی است."];
        if (!settings.ShowEmail && !string.IsNullOrWhiteSpace(command.AuthorEmail))
            fieldErrors[nameof(command.AuthorEmail)] = ["ارسال ایمیل مجاز نیست."];
        if (!settings.ShowPhone && !string.IsNullOrWhiteSpace(command.AuthorPhone))
            fieldErrors[nameof(command.AuthorPhone)] = ["ارسال تلفن مجاز نیست."];
        if (fieldErrors.Count > 0)
            throw new DomainValidationException(fieldErrors);

        if (settings.EnableCaptcha)
        {
            var captchaResult = await _captcha.ValidateAsync(
                new CommentCaptchaValidationRequest(
                    settings.CaptchaProvider,
                    command.CaptchaToken,
                    command.RemoteIp),
                cancellationToken);
            if (!captchaResult.Succeeded)
            {
                throw new DomainValidationException(new Dictionary<string, string[]>
                {
                    ["Captcha"] = [captchaResult.ErrorMessage ?? "کپچا نامعتبر است."]
                });
            }
        }

        var comment = Comment.Create(
            command.TargetType,
            command.TargetId,
            command.TargetTitle,
            command.UserId,
            command.AuthorName,
            settings.ShowEmail ? command.AuthorEmail : null,
            settings.ShowPhone ? command.AuthorPhone : null,
            command.Body);

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), id);
        comment.Approve();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), id);
        comment.Reject();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Guid id, UpdateCommentCommand command, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
        }

        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), id);

        comment.UpdateContent(command.Body, command.PublishedAtUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Comment), id);
        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
