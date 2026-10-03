using CMS.Modules.Comments.Application.Interfaces;
using CMS.Modules.Comments.Application.Settings;
using CMS.Modules.Comments.Domain.Entities;
using CMS.Modules.Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CMS.Modules.Comments.Infrastructure.Services;

public sealed class CommentSettingsService : ICommentSettingsService
{
    private const string CacheKey = "comments:settings";
    private readonly CommentsDbContext _db;
    private readonly IMemoryCache _cache;

    public CommentSettingsService(CommentsDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<CommentSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out CommentSettingsDto? cached) && cached is not null)
            return cached;

        var entity = await EnsureSettingsAsync(cancellationToken);
        var dto = Map(entity);
        _cache.Set(CacheKey, dto, TimeSpan.FromMinutes(5));
        return dto;
    }

    public async Task UpdateAsync(UpdateCommentSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await EnsureSettingsAsync(cancellationToken);
        entity.Update(
            command.AllowAnonymous,
            command.EnableOnBlog,
            command.EnableOnEvents,
            command.EnableOnProducts,
            command.EnableOnProductCategories,
            command.ShowEmail,
            command.RequireEmail,
            command.ShowPhone,
            command.RequirePhone,
            command.EnableCaptcha,
            command.CaptchaProvider);

        await _db.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey);
    }

    private async Task<CommentSettings> EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        var entity = await _db.Settings.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (entity is not null)
            return entity;

        entity = CommentSettings.CreateDefault();
        _db.Settings.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private static CommentSettingsDto Map(CommentSettings s) =>
        new(
            s.AllowAnonymous,
            s.EnableOnBlog,
            s.EnableOnEvents,
            s.EnableOnProducts,
            s.EnableOnProductCategories,
            s.ShowEmail,
            s.RequireEmail,
            s.ShowPhone,
            s.RequirePhone,
            s.EnableCaptcha,
            s.CaptchaProvider);
}
