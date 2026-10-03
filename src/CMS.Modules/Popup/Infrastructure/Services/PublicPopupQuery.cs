using CMS.Modules.Popup.Application.Interfaces;
using CMS.Modules.Popup.Application.Popups;
using CMS.Modules.Popup.Domain.Entities;
using CMS.Modules.Popup.Domain.Enums;
using CMS.Modules.Popup.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CMS.Modules.Popup.Infrastructure.Services;

public sealed class PublicPopupQuery : IPublicPopupQuery
{
    private readonly PopupDbContext _db;

    public PublicPopupQuery(PopupDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicPopupDto>> GetForPathAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var normalized = PopupPageMatcher.NormalizePath(path);
        var candidates = await _db.Popups
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var targetSlugs = candidates.ToDictionary(p => p.Id, p => p.Slug);

        // Mount every active popup on every page so Core openers
        // (data-popup-open / TriggerSelector / CTA → popup) work site-wide.
        // Timer/scroll auto-open still respects page targeting.
        var result = new List<PublicPopupDto>(candidates.Count);
        foreach (var p in candidates)
        {
            var matchesPage = MatchesPage(p.PageTargetMode, p.PagePaths, normalized);
            result.Add(Map(p, targetSlugs, suppressAutoTrigger: !matchesPage));
        }

        return result;
    }

    private static PublicPopupDto Map(
        PopupItem p,
        IReadOnlyDictionary<Guid, string> targetSlugs,
        bool suppressAutoTrigger)
    {
        string? targetSlug = null;
        if (p.CtaTargetPopupId.HasValue)
            targetSlugs.TryGetValue(p.CtaTargetPopupId.Value, out targetSlug);

        var triggerType = suppressAutoTrigger ? PopupTriggerTypes.Manual : p.TriggerType;

        return new PublicPopupDto(
            p.Id,
            p.Title,
            p.Slug,
            p.BodyText,
            p.ImageUrl,
            p.ContentHtml,
            p.FormId,
            p.CtaText,
            p.CtaAction,
            p.CtaUrl,
            p.CtaTargetPopupId,
            targetSlug,
            triggerType,
            suppressAutoTrigger ? null : p.TriggerDelaySeconds,
            suppressAutoTrigger ? null : p.TriggerScrollPercent,
            p.TriggerSelector,
            suppressAutoTrigger ? null : p.TriggerConfigJson,
            p.ShowOverlay,
            p.ShowCloseButton,
            p.CloseOnOverlayClick,
            p.CloseOnEscape,
            p.LockBodyScroll,
            p.EnableContentScroll,
            p.Frequency,
            p.ExtensionSettingsJson,
            p.SortOrder);
    }

    private static bool MatchesPage(PopupPageTargetMode mode, string pagePaths, string path) =>
        mode switch
        {
            PopupPageTargetMode.All => true,
            PopupPageTargetMode.Homepage => PopupPageMatcher.IsHomepage(path),
            PopupPageTargetMode.Include => PopupPageMatcher.MatchesAny(path, PopupPageMatcher.ParsePaths(pagePaths)),
            PopupPageTargetMode.Exclude => !PopupPageMatcher.MatchesAny(path, PopupPageMatcher.ParsePaths(pagePaths)),
            _ => false
        };
}
