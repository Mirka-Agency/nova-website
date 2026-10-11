using System.Text;
using CMS.Application.Security;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Application.Import;
using CMS.Modules.Comments.Domain.Entities;
using CMS.Modules.Comments.Domain.Enums;
using CMS.Modules.Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMS.Modules.Comments.Infrastructure.Services;

public sealed class CommentImportService : ICommentImportService
{
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private readonly CommentsDbContext _db;
    private readonly IWordPressCommentXmlParser _parser;
    private readonly ICommentUrlMatcher _urlMatcher;
    private readonly ICommentTargetCatalog _catalog;
    private readonly ICommentImportPreviewStore _previewStore;
    private readonly IHtmlContentSanitizer _sanitizer;
    private readonly CommentImportOptions _options;
    private readonly ILogger<CommentImportService> _logger;

    public CommentImportService(
        CommentsDbContext db,
        IWordPressCommentXmlParser parser,
        ICommentUrlMatcher urlMatcher,
        ICommentTargetCatalog catalog,
        ICommentImportPreviewStore previewStore,
        IHtmlContentSanitizer sanitizer,
        IOptions<CommentImportOptions> options,
        ILogger<CommentImportService> logger)
    {
        _db = db;
        _parser = parser;
        _urlMatcher = urlMatcher;
        _catalog = catalog;
        _previewStore = previewStore;
        _sanitizer = sanitizer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CommentImportPreview> BuildPreviewAsync(
        Stream xmlStream,
        CancellationToken cancellationToken = default)
    {
        var parsed = _parser.Parse(xmlStream);
        var catalog = await _catalog.ListAsync(cancellationToken);
        var trustedHosts = BuildTrustedHosts();
        var existingIds = await LoadExistingExternalIdsAsync(parsed, cancellationToken);

        var rows = new List<CommentImportPreviewRow>(parsed.Count);
        foreach (var item in parsed)
        {
            rows.Add(BuildPreviewRow(item, catalog, trustedHosts, existingIds));
        }

        var preview = new CommentImportPreview(
            PreviewId: Guid.NewGuid().ToString("N"),
            CreatedAtUtc: DateTime.UtcNow,
            TotalCount: rows.Count,
            ReadyCount: rows.Count(r => r.Disposition == CommentImportRowDisposition.Ready),
            DuplicateCount: rows.Count(r => r.Disposition == CommentImportRowDisposition.Duplicate),
            UnmatchedCount: rows.Count(r => r.Disposition == CommentImportRowDisposition.Unmatched),
            AmbiguousCount: rows.Count(r => r.Disposition == CommentImportRowDisposition.Ambiguous),
            InvalidCount: rows.Count(r => r.Disposition == CommentImportRowDisposition.Invalid),
            Rows: rows);

        await _previewStore.SaveAsync(preview, cancellationToken);
        return preview;
    }

    public Task<CommentImportPreview?> GetPreviewAsync(
        string previewId,
        CancellationToken cancellationToken = default) =>
        _previewStore.GetAsync(previewId, cancellationToken);

    public async Task<CommentImportResult> ConfirmAsync(
        CommentImportConfirmCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.PreviewId))
            throw new DomainException("شناسه پیش‌نمایش نامعتبر است.");

        var preview = await _previewStore.GetAsync(command.PreviewId, cancellationToken)
            ?? throw new DomainException("پیش‌نمایش منقضی شده یا یافت نشد. لطفاً فایل را دوباره آپلود کنید.");

        // Always import synchronously so the admin result reflects real DB writes.
        // (Background queue previously reported success while retries/transactions rolled back.)
        return await ExecuteImportAsync(command, cancellationToken);
    }

    internal async Task<CommentImportResult> ExecuteImportAsync(
        CommentImportConfirmCommand command,
        CancellationToken cancellationToken)
    {
        var preview = await _previewStore.GetAsync(command.PreviewId, cancellationToken)
            ?? throw new DomainException("پیش‌نمایش منقضی شده یا یافت نشد.");

        var overrides = (command.ManualOverrides ?? [])
            .Where(o => !string.IsNullOrWhiteSpace(o.ExternalId) && o.TargetId != Guid.Empty)
            .GroupBy(o => o.ExternalId.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

        var catalog = await _catalog.ListAsync(cancellationToken);
        var catalogByKey = catalog.ToDictionary(
            c => (c.TargetType, c.TargetId),
            c => c);

        var workItems = new List<ImportWorkItem>();
        var results = new List<CommentImportRowResult>();

        foreach (var row in preview.Rows)
        {
            if (command.OnlyNew && row.Disposition == CommentImportRowDisposition.Duplicate)
            {
                results.Add(new CommentImportRowResult(
                    row.ExternalId, row.PageUrl, "duplicate", "قبلاً وارد شده است.", null, row.MatchedTargetTitle));
                continue;
            }

            if (row.Disposition == CommentImportRowDisposition.Invalid)
            {
                results.Add(new CommentImportRowResult(
                    row.ExternalId, row.PageUrl, "failed", row.ErrorMessage ?? "نامعتبر", null, null));
                continue;
            }

            CommentTargetType? targetType = row.MatchedTargetType;
            Guid? targetId = row.MatchedTargetId;
            string? targetTitle = row.MatchedTargetTitle;

            if (overrides.TryGetValue(row.ExternalId, out var manual))
            {
                targetType = manual.TargetType;
                targetId = manual.TargetId;
                targetTitle = manual.TargetTitle;
                if (string.IsNullOrWhiteSpace(targetTitle)
                    && catalogByKey.TryGetValue((manual.TargetType, manual.TargetId), out var candidate))
                {
                    targetTitle = candidate.Title;
                }
            }

            if (targetType is null || targetId is null || targetId == Guid.Empty || string.IsNullOrWhiteSpace(targetTitle))
            {
                results.Add(new CommentImportRowResult(
                    row.ExternalId, row.PageUrl, "unmatched", "صفحه مقصد یافت نشد.", null, null));
                continue;
            }

            if (row.Disposition == CommentImportRowDisposition.Duplicate && !command.OnlyNew)
            {
                results.Add(new CommentImportRowResult(
                    row.ExternalId, row.PageUrl, "duplicate", "قبلاً وارد شده است.", null, targetTitle));
                continue;
            }

            // Re-check DB for idempotency under concurrency.
            var exists = await _db.Comments.AsNoTracking().AnyAsync(
                c => c.SourceSystem == Comment.WordPressSourceSystem
                     && c.SourceExternalId == row.ExternalId,
                cancellationToken);
            if (exists)
            {
                results.Add(new CommentImportRowResult(
                    row.ExternalId, row.PageUrl, "duplicate", "قبلاً وارد شده است.", null, targetTitle));
                continue;
            }

            workItems.Add(new ImportWorkItem(row, targetType.Value, targetId.Value, targetTitle!));
        }

        var idMap = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var ordered = OrderParentsFirst(workItems);
        var batchSize = Math.Clamp(_options.BatchSize, 20, 500);

        // IMPORTANT: do not use user transactions here — CommentsDbContext enables
        // EnableRetryOnFailure, which rejects BeginTransactionAsync and caused every
        // import batch to roll back while the UI still reported "imported".
        for (var offset = 0; offset < ordered.Count; offset += batchSize)
        {
            var batch = ordered.Skip(offset).Take(batchSize).ToList();
            var pending = new List<(ImportWorkItem Item, Comment Entity)>();

            foreach (var item in batch)
            {
                try
                {
                    var body = SanitizeBody(item.Row.Body);
                    if (body.Length > 4000)
                    {
                        results.Add(new CommentImportRowResult(
                            item.Row.ExternalId,
                            item.Row.PageUrl,
                            "failed",
                            "متن کامنت بیش از ۴۰۰۰ نویسه است.",
                            null,
                            item.TargetTitle));
                        continue;
                    }

                    Guid? parentId = null;
                    if (!string.IsNullOrWhiteSpace(item.Row.ExternalParentId))
                    {
                        if (idMap.TryGetValue(item.Row.ExternalParentId, out var mappedParent))
                        {
                            parentId = mappedParent;
                        }
                        else
                        {
                            parentId = await _db.Comments.AsNoTracking()
                                .Where(c =>
                                    c.SourceSystem == Comment.WordPressSourceSystem
                                    && c.SourceExternalId == item.Row.ExternalParentId)
                                .Select(c => (Guid?)c.Id)
                                .FirstOrDefaultAsync(cancellationToken);
                        }
                    }

                    var comment = Comment.Import(
                        item.TargetType,
                        item.TargetId,
                        item.TargetTitle,
                        item.Row.AuthorName,
                        item.Row.AuthorEmail,
                        body,
                        CommentStatus.Approved,
                        item.Row.PublishedAtUtc,
                        Comment.WordPressSourceSystem,
                        item.Row.ExternalId,
                        item.Row.PageUrl,
                        parentId);

                    _db.Comments.Add(comment);
                    pending.Add((item, comment));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed preparing WordPress comment {ExternalId}", item.Row.ExternalId);
                    results.Add(new CommentImportRowResult(
                        item.Row.ExternalId,
                        item.Row.PageUrl,
                        "failed",
                        ex.Message,
                        null,
                        item.TargetTitle));
                }
            }

            if (pending.Count == 0)
                continue;

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                foreach (var (item, entity) in pending)
                {
                    idMap[item.Row.ExternalId] = entity.Id;
                    results.Add(new CommentImportRowResult(
                        item.Row.ExternalId,
                        item.Row.PageUrl,
                        "imported",
                        null,
                        entity.Id,
                        item.TargetTitle));
                }
            }
            catch (Exception batchEx)
            {
                _logger.LogError(batchEx, "Comment import batch SaveChanges failed; retrying one-by-one");
                _db.ChangeTracker.Clear();

                foreach (var (item, _) in pending)
                {
                    try
                    {
                        var body = SanitizeBody(item.Row.Body);
                        Guid? parentId = null;
                        if (!string.IsNullOrWhiteSpace(item.Row.ExternalParentId)
                            && idMap.TryGetValue(item.Row.ExternalParentId, out var mappedParent))
                        {
                            parentId = mappedParent;
                        }

                        var comment = Comment.Import(
                            item.TargetType,
                            item.TargetId,
                            item.TargetTitle,
                            item.Row.AuthorName,
                            item.Row.AuthorEmail,
                            body,
                            CommentStatus.Approved,
                            item.Row.PublishedAtUtc,
                            Comment.WordPressSourceSystem,
                            item.Row.ExternalId,
                            item.Row.PageUrl,
                            parentId);

                        _db.Comments.Add(comment);
                        await _db.SaveChangesAsync(cancellationToken);
                        idMap[item.Row.ExternalId] = comment.Id;
                        results.Add(new CommentImportRowResult(
                            item.Row.ExternalId,
                            item.Row.PageUrl,
                            "imported",
                            null,
                            comment.Id,
                            item.TargetTitle));
                    }
                    catch (Exception ex)
                    {
                        _db.ChangeTracker.Clear();
                        _logger.LogWarning(ex, "Failed importing WordPress comment {ExternalId}", item.Row.ExternalId);
                        results.Add(new CommentImportRowResult(
                            item.Row.ExternalId,
                            item.Row.PageUrl,
                            "failed",
                            ex.Message,
                            null,
                            item.TargetTitle));
                    }
                }
            }
        }

        // Second pass: attach parents that were imported later in another batch.
        await AttachMissingParentsAsync(ordered, idMap, cancellationToken);

        // Import policy: every comment must be publicly visible after import.
        await _db.Comments
            .Where(c => c.Status != CommentStatus.Approved)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, CommentStatus.Approved)
                    .SetProperty(c => c.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);

        var result = new CommentImportResult(
            PreviewId: preview.PreviewId,
            TotalCount: preview.TotalCount,
            ImportedCount: results.Count(r => r.Outcome == "imported"),
            DuplicateCount: results.Count(r => r.Outcome == "duplicate"),
            UnmatchedCount: results.Count(r => r.Outcome == "unmatched"),
            FailedCount: results.Count(r => r.Outcome == "failed"),
            Queued: false,
            Rows: results);

        await _previewStore.RemoveAsync(preview.PreviewId, cancellationToken);
        return result;
    }

    public byte[] BuildCsvReport(CommentImportResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ExternalId,PageUrl,Outcome,Message,CommentId,TargetTitle");
        foreach (var row in result.Rows)
        {
            sb.Append(EscapeCsv(row.ExternalId)).Append(',')
                .Append(EscapeCsv(row.PageUrl)).Append(',')
                .Append(EscapeCsv(row.Outcome)).Append(',')
                .Append(EscapeCsv(row.Message)).Append(',')
                .Append(EscapeCsv(row.CommentId?.ToString())).Append(',')
                .Append(EscapeCsv(row.TargetTitle))
                .AppendLine();
        }

        return Utf8Bom.GetBytes(sb.ToString());
    }

    public byte[] GetSampleXml()
    {
        const string sample =
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <comments>
              <comment>
                <comment_id>101</comment_id>
                <comment_author>علی رضایی</comment_author>
                <comment_author_email>ali@example.com</comment_author_email>
                <comment_content>مقاله مفیدی بود، ممنون.</comment_content>
                <comment_date_gmt>2023-05-10 08:30:00</comment_date_gmt>
                <comment_approved>1</comment_approved>
                <comment_parent>0</comment_parent>
                <page_url>https://old.example.com/sample-post/</page_url>
              </comment>
              <comment>
                <comment_id>102</comment_id>
                <comment_author>سارا محمدی</comment_author>
                <comment_author_email>sara@example.com</comment_author_email>
                <comment_content>کاملاً موافقم.</comment_content>
                <comment_date_gmt>2023-05-11 09:15:00</comment_date_gmt>
                <comment_approved>1</comment_approved>
                <comment_parent>101</comment_parent>
                <page_url>https://old.example.com/sample-post/</page_url>
              </comment>
              <comment>
                <comment_id>201</comment_id>
                <comment_author>مهمان</comment_author>
                <comment_author_email></comment_author_email>
                <comment_content>این URL در CMS وجود ندارد و باید دستی وصل شود.</comment_content>
                <comment_date_gmt>2023-06-01 12:00:00</comment_date_gmt>
                <comment_approved>0</comment_approved>
                <comment_parent>0</comment_parent>
                <page_url>https://old.example.com/missing-page/</page_url>
              </comment>
            </comments>
            """;
        return Encoding.UTF8.GetBytes(sample.Trim() + Environment.NewLine);
    }

    private CommentImportPreviewRow BuildPreviewRow(
        WordPressCommentXmlItem item,
        IReadOnlyList<CommentTargetCandidate> catalog,
        IReadOnlyCollection<string> trustedHosts,
        HashSet<string> existingIds)
    {
        try
        {
            var body = SanitizeBody(item.Body);
            if (string.IsNullOrWhiteSpace(body))
            {
                return InvalidRow(item, "/", "متن کامنت پس از پاک‌سازی خالی شد.");
            }

            if (body.Length > 4000)
            {
                return InvalidRow(item, _urlMatcher.NormalizePath(item.PageUrl, trustedHosts) ?? "/",
                    "متن کامنت بیش از ۴۰۰۰ نویسه است.");
            }

            var path = _urlMatcher.NormalizePath(item.PageUrl, trustedHosts) ?? string.Empty;
            if (existingIds.Contains(item.ExternalId))
            {
                return new CommentImportPreviewRow(
                    item.ExternalId,
                    item.ExternalParentId,
                    item.AuthorName,
                    item.AuthorEmail,
                    body,
                    item.PublishedAtUtc,
                    CommentStatus.Approved,
                    item.PageUrl,
                    path,
                    CommentImportRowDisposition.Duplicate,
                    null,
                    null,
                    null,
                    "قبلاً وارد شده است.");
            }

            var match = _urlMatcher.Match(item.PageUrl, catalog, trustedHosts);
            return match.Kind switch
            {
                CommentUrlMatchKind.Exact when match.Target is not null => new CommentImportPreviewRow(
                    item.ExternalId,
                    item.ExternalParentId,
                    item.AuthorName,
                    item.AuthorEmail,
                    body,
                    item.PublishedAtUtc,
                    CommentStatus.Approved,
                    item.PageUrl,
                    path,
                    CommentImportRowDisposition.Ready,
                    match.Target.TargetType,
                    match.Target.TargetId,
                    match.Target.Title,
                    null),
                CommentUrlMatchKind.Ambiguous => new CommentImportPreviewRow(
                    item.ExternalId,
                    item.ExternalParentId,
                    item.AuthorName,
                    item.AuthorEmail,
                    body,
                    item.PublishedAtUtc,
                    CommentStatus.Approved,
                    item.PageUrl,
                    path,
                    CommentImportRowDisposition.Ambiguous,
                    null,
                    null,
                    null,
                    "چند صفحه با این URL مطابقت دارند؛ انتخاب خودکار انجام نشد."),
                _ => new CommentImportPreviewRow(
                    item.ExternalId,
                    item.ExternalParentId,
                    item.AuthorName,
                    item.AuthorEmail,
                    body,
                    item.PublishedAtUtc,
                    CommentStatus.Approved,
                    item.PageUrl,
                    path,
                    CommentImportRowDisposition.Unmatched,
                    null,
                    null,
                    null,
                    "هیچ صفحه/مقاله‌ای با این URL یافت نشد.")
            };
        }
        catch (Exception ex)
        {
            return InvalidRow(item, "/", ex.Message);
        }
    }

    private static CommentImportPreviewRow InvalidRow(WordPressCommentXmlItem item, string path, string error) =>
        new(
            item.ExternalId,
            item.ExternalParentId,
            item.AuthorName,
            item.AuthorEmail,
            item.Body,
            item.PublishedAtUtc,
            item.Status,
            item.PageUrl,
            path,
            CommentImportRowDisposition.Invalid,
            null,
            null,
            null,
            error);

    private string SanitizeBody(string body)
    {
        // Comments are rendered as plain text; strip HTML after sanitizer allow-list pass.
        var sanitized = _sanitizer.Sanitize(body);
        return WordPressPlainText.FromHtml(sanitized);
    }

    private async Task<HashSet<string>> LoadExistingExternalIdsAsync(
        IReadOnlyList<WordPressCommentXmlItem> parsed,
        CancellationToken cancellationToken)
    {
        var ids = parsed.Select(p => p.ExternalId).Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
            return [];

        var existing = new HashSet<string>(StringComparer.Ordinal);
        const int chunk = 500;
        for (var i = 0; i < ids.Count; i += chunk)
        {
            var slice = ids.Skip(i).Take(chunk).ToList();
            var found = await _db.Comments.AsNoTracking()
                .Where(c => c.SourceSystem == Comment.WordPressSourceSystem && c.SourceExternalId != null && slice.Contains(c.SourceExternalId))
                .Select(c => c.SourceExternalId!)
                .ToListAsync(cancellationToken);
            foreach (var id in found)
                existing.Add(id);
        }

        return existing;
    }

    private IReadOnlyCollection<string> BuildTrustedHosts()
    {
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var host in _options.LegacyHosts.Concat(_options.CurrentHosts))
        {
            if (!string.IsNullOrWhiteSpace(host))
                hosts.Add(host.Trim());
        }

        return hosts;
    }

    private static int CountImportable(CommentImportPreview preview, CommentImportConfirmCommand command)
    {
        var overrideIds = new HashSet<string>(
            (command.ManualOverrides ?? []).Select(o => o.ExternalId),
            StringComparer.Ordinal);

        return preview.Rows.Count(r =>
            r.Disposition == CommentImportRowDisposition.Ready
            || (overrideIds.Contains(r.ExternalId)
                && r.Disposition is CommentImportRowDisposition.Unmatched or CommentImportRowDisposition.Ambiguous));
    }

    private static List<ImportWorkItem> OrderParentsFirst(List<ImportWorkItem> items)
    {
        var byId = items.ToDictionary(i => i.Row.ExternalId, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<ImportWorkItem>(items.Count);

        void Visit(ImportWorkItem item)
        {
            if (visited.Contains(item.Row.ExternalId))
                return;
            if (!visiting.Add(item.Row.ExternalId))
                return;

            if (!string.IsNullOrWhiteSpace(item.Row.ExternalParentId)
                && byId.TryGetValue(item.Row.ExternalParentId, out var parent))
            {
                Visit(parent);
            }

            visiting.Remove(item.Row.ExternalId);
            visited.Add(item.Row.ExternalId);
            ordered.Add(item);
        }

        foreach (var item in items.OrderBy(i => i.Row.PublishedAtUtc))
            Visit(item);

        return ordered;
    }

    private async Task AttachMissingParentsAsync(
        List<ImportWorkItem> ordered,
        Dictionary<string, Guid> idMap,
        CancellationToken cancellationToken)
    {
        var needsUpdate = false;
        foreach (var item in ordered)
        {
            if (string.IsNullOrWhiteSpace(item.Row.ExternalParentId))
                continue;
            if (!idMap.TryGetValue(item.Row.ExternalId, out var childId))
                continue;
            if (!idMap.TryGetValue(item.Row.ExternalParentId, out var parentId))
                continue;

            var entity = await _db.Comments.FirstOrDefaultAsync(c => c.Id == childId, cancellationToken);
            if (entity is null || entity.ParentId == parentId)
                continue;

            entity.SetParent(parentId);
            needsUpdate = true;
        }

        if (needsUpdate)
            await _db.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private sealed record ImportWorkItem(
        CommentImportPreviewRow Row,
        CommentTargetType TargetType,
        Guid TargetId,
        string TargetTitle);
}

internal static class WordPressPlainText
{
    private static readonly System.Text.RegularExpressions.Regex HtmlTagRegex =
        new("<.*?>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.Compiled);

    public static string FromHtml(string raw)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(raw);
        decoded = HtmlTagRegex.Replace(decoded ?? string.Empty, " ");
        decoded = System.Net.WebUtility.HtmlDecode(decoded);
        return string.Join(' ', decoded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
