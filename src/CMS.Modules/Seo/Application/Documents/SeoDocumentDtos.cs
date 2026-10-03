namespace CMS.Modules.Seo.Application.Documents;

public sealed record SeoDocumentDto(
    Guid Id,
    string ContentType,
    Guid ContentId,
    string? FocusKeyword,
    bool RobotsIndex,
    bool RobotsFollow,
    string? SchemaType,
    string? SchemaJson,
    int? SeoScore,
    string? AnalysisJson,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SaveSeoDocumentCommand(
    string ContentType,
    Guid ContentId,
    string? FocusKeyword,
    bool RobotsIndex,
    bool RobotsFollow,
    string? SchemaType,
    string? SchemaJson = null,
    int? SeoScore = null,
    string? AnalysisJson = null);
