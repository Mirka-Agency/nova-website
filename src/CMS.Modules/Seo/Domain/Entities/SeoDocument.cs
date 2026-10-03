using CMS.Domain.Common;
using CMS.Domain.Exceptions;
using CMS.Modules.Seo.Domain.Enums;

namespace CMS.Modules.Seo.Domain.Entities;

public class SeoDocument : BaseEntity
{
    private SeoDocument()
    {
    }

    public string ContentType { get; private set; } = string.Empty;
    public Guid ContentId { get; private set; }
    public string? FocusKeyword { get; private set; }
    public bool RobotsIndex { get; private set; } = true;
    public bool RobotsFollow { get; private set; } = true;
    public string? SchemaType { get; private set; }
    public string? SchemaJson { get; private set; }
    public int? SeoScore { get; private set; }
    public string? AnalysisJson { get; private set; }

    public static SeoDocument Create(
        string contentType,
        Guid contentId,
        string? focusKeyword,
        bool robotsIndex,
        bool robotsFollow,
        string? schemaType,
        string? schemaJson = null,
        int? seoScore = null,
        string? analysisJson = null)
    {
        var doc = new SeoDocument();
        doc.Apply(contentType, contentId, focusKeyword, robotsIndex, robotsFollow, schemaType, schemaJson, seoScore, analysisJson);
        return doc;
    }

    public void Update(
        string? focusKeyword,
        bool robotsIndex,
        bool robotsFollow,
        string? schemaType,
        string? schemaJson = null,
        int? seoScore = null,
        string? analysisJson = null)
    {
        Apply(ContentType, ContentId, focusKeyword, robotsIndex, robotsFollow, schemaType, schemaJson, seoScore, analysisJson);
        Touch();
    }

    private void Apply(
        string contentType,
        Guid contentId,
        string? focusKeyword,
        bool robotsIndex,
        bool robotsFollow,
        string? schemaType,
        string? schemaJson,
        int? seoScore,
        string? analysisJson)
    {
        Validate(contentType, contentId, focusKeyword, schemaType, schemaJson, seoScore);

        ContentType = SeoContentTypes.Normalize(contentType);
        ContentId = contentId;
        FocusKeyword = string.IsNullOrWhiteSpace(focusKeyword) ? null : focusKeyword.Trim();
        RobotsIndex = robotsIndex;
        RobotsFollow = robotsFollow;
        SchemaType = SeoSchemaTypes.Normalize(schemaType);
        SchemaJson = string.IsNullOrWhiteSpace(schemaJson) ? null : schemaJson.Trim();
        SeoScore = seoScore;
        AnalysisJson = string.IsNullOrWhiteSpace(analysisJson) ? null : analysisJson.Trim();
    }

    private static void Validate(
        string contentType,
        Guid contentId,
        string? focusKeyword,
        string? schemaType,
        string? schemaJson,
        int? seoScore)
    {
        if (contentId == Guid.Empty)
            throw new DomainException("شناسه محتوا الزامی است.");

        if (!SeoContentTypes.IsKnown(contentType))
            throw new DomainException("نوع محتوا نامعتبر است.");

        if (!string.IsNullOrWhiteSpace(focusKeyword) && focusKeyword.Trim().Length > 200)
            throw new DomainException("کلمه کلیدی تمرکز خیلی طولانی است.");

        if (!string.IsNullOrWhiteSpace(schemaType) && SeoSchemaTypes.Normalize(schemaType) is null)
            throw new DomainException("نوع اسکیما نامعتبر است.");

        if (seoScore is < 0 or > 100)
            throw new DomainException("امتیاز سئو باید بین ۰ تا ۱۰۰ باشد.");

        if (!string.IsNullOrWhiteSpace(schemaJson) && schemaJson.Length > 100_000)
            throw new DomainException("JSON اسکیما خیلی طولانی است.");
    }
}
