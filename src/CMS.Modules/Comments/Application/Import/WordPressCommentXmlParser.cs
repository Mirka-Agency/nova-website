using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CMS.Domain.Exceptions;
using CMS.Modules.Comments.Domain.Enums;

namespace CMS.Modules.Comments.Application.Import;

public sealed class WordPressCommentXmlParser : IWordPressCommentXmlParser
{
    private static readonly Regex HtmlTagRegex = new("<.*?>", RegexOptions.Singleline | RegexOptions.Compiled);

    public IReadOnlyList<WordPressCommentXmlItem> Parse(Stream xmlStream)
    {
        ArgumentNullException.ThrowIfNull(xmlStream);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
            MaxCharactersFromEntities = 1024,
            MaxCharactersInDocument = 50_000_000
        };

        try
        {
            using var reader = XmlReader.Create(xmlStream, settings);
            var document = new XmlDocument { XmlResolver = null };
            document.Load(reader);

            var commentNodes = document.SelectNodes("//*[local-name()='comment']");
            if (commentNodes is null || commentNodes.Count == 0)
                throw new DomainException("هیچ کامنتی در فایل XML یافت نشد.");

            var items = new List<WordPressCommentXmlItem>(commentNodes.Count);
            var index = 0;
            foreach (XmlNode node in commentNodes)
            {
                index++;
                try
                {
                    var externalId = FirstValue(node, "comment_id", "id", "commentId");
                    if (string.IsNullOrWhiteSpace(externalId))
                        continue;

                    var author = FirstValue(node, "comment_author", "author", "author_name") ?? "مهمان";
                    var email = FirstValue(node, "comment_author_email", "author_email", "email");
                    var bodyRaw = FirstValue(node, "comment_content", "content", "body") ?? string.Empty;
                    var body = ToPlainText(bodyRaw);
                    if (string.IsNullOrWhiteSpace(body))
                        continue;

                    var dateRaw = FirstValue(node, "comment_date_gmt", "comment_date", "date_gmt", "date", "published_at");
                    if (!TryParseDate(dateRaw, out var publishedAtUtc))
                        continue;

                    var approvedRaw = FirstValue(node, "comment_approved", "approved", "status");
                    var status = MapStatus(approvedRaw);

                    var parent = FirstValue(node, "comment_parent", "parent", "parent_id");
                    if (string.IsNullOrWhiteSpace(parent) || parent is "0")
                        parent = null;

                    var pageUrl = FirstValue(node, "page_url", "comment_post_url", "post_url", "url", "link");
                    if (string.IsNullOrWhiteSpace(pageUrl))
                        continue;

                    items.Add(new WordPressCommentXmlItem(
                        ExternalId: externalId.Trim(),
                        ExternalParentId: parent?.Trim(),
                        AuthorName: author.Trim(),
                        AuthorEmail: string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                        Body: body,
                        PublishedAtUtc: publishedAtUtc,
                        Status: status,
                        PageUrl: pageUrl.Trim(),
                        LineHint: index));
                }
                catch
                {
                    // Skip malformed comment nodes; one bad row must not abort the whole file.
                }
            }

            if (items.Count == 0)
                throw new DomainException("هیچ کامنت معتبری در فایل XML یافت نشد.");

            return items;
        }
        catch (XmlException ex)
        {
            throw new DomainException("ساختار فایل XML نامعتبر است: " + ex.Message);
        }
    }

    private static string? FirstValue(XmlNode parent, params string[] localNames)
    {
        foreach (var name in localNames)
        {
            var node = parent.SelectSingleNode($"*[local-name()='{name}']");
            if (node is null)
                continue;

            var value = node.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static CommentStatus MapStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return CommentStatus.Pending;

        return raw.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "approve" or "approved" => CommentStatus.Approved,
            "spam" or "trash" or "reject" or "rejected" => CommentStatus.Rejected,
            _ => CommentStatus.Pending
        };
    }

    private static bool TryParseDate(string? raw, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(
                raw.Trim(),
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        if (DateTime.TryParse(
                raw.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        return false;
    }

    private static string ToPlainText(string raw)
    {
        var decoded = WebUtility.HtmlDecode(raw);
        decoded = HtmlTagRegex.Replace(decoded, " ");
        decoded = WebUtility.HtmlDecode(decoded);
        var builder = new StringBuilder(decoded.Length);
        var previousWhitespace = false;
        foreach (var ch in decoded)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!previousWhitespace)
                {
                    builder.Append(' ');
                    previousWhitespace = true;
                }
            }
            else
            {
                builder.Append(ch);
                previousWhitespace = false;
            }
        }

        return builder.ToString().Trim();
    }
}
