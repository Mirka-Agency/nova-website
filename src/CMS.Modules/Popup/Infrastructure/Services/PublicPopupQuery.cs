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

        var byId = candidates.ToDictionary(p => p.Id);
        var targetSlugs = candidates.ToDictionary(p => p.Id, p => p.Slug);

        var result = new List<PublicPopupDto>(candidates.Count);
        var includedIds = new HashSet<Guid>();

        foreach (var p in candidates)
        {
            if (!MatchesPage(p.PageTargetMode, p.PagePaths, normalized))
                continue;

            result.Add(Map(p, targetSlugs, forceManualTrigger: false));
            includedIds.Add(p.Id);
        }

        // CTA "open another popup" targets must be in the host even when they do not
        // match the current path — otherwise the button has nowhere to open.
        AppendCtaTargets(result, includedIds, byId, targetSlugs);

        return result;
    }

    private static void AppendCtaTargets(
        List<PublicPopupDto> result,
        HashSet<Guid> includedIds,
        IReadOnlyDictionary<Guid, PopupItem> byId,
        IReadOnlyDictionary<Guid, string> targetSlugs)
    {
        var pending = new Queue<Guid>();
        foreach (var dto in result)
            EnqueueMissingTarget(dto.CtaAction, dto.CtaTargetPopupId, includedIds, pending);

        while (pending.Count > 0)
        {
            var id = pending.Dequeue();
            if (!includedIds.Add(id))
                continue;
            if (!byId.TryGetValue(id, out var target))
                continue;

            var dto = Map(target, targetSlugs, forceManualTrigger: true);
            result.Add(dto);
            EnqueueMissingTarget(dto.CtaAction, dto.CtaTargetPopupId, includedIds, pending);
        }
    }

    private static void EnqueueMissingTarget(
        string ctaAction,
        Guid? ctaTargetPopupId,
        HashSet<Guid> includedIds,
        Queue<Guid> pending)
    {
        if (PopupCtaActions.Normalize(ctaAction) != PopupCtaActions.OpenPopup)
            return;
        if (ctaTargetPopupId is not Guid targetId || includedIds.Contains(targetId))
            return;
        pending.Enqueue(targetId);
    }

    private static PublicPopupDto Map(
        PopupItem p,
        IReadOnlyDictionary<Guid, string> targetSlugs,
        bool forceManualTrigger)
    {
        string? targetSlug = null;
        if (p.CtaTargetPopupId.HasValue)
            targetSlugs.TryGetValue(p.CtaTargetPopupId.Value, out targetSlug);

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
            forceManualTrigger ? PopupTriggerTypes.Manual : p.TriggerType,
            forceManualTrigger ? null : p.TriggerDelaySeconds,
            forceManualTrigger ? null : p.TriggerScrollPercent,
            forceManualTrigger ? null : p.TriggerSelector,
            forceManualTrigger ? null : p.TriggerConfigJson,
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
