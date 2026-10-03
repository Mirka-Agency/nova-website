namespace CMS.Modules.Seo.Application.Tools;

public sealed record BrokenLinkCheckResult(
    string Url,
    int? StatusCode,
    bool IsOk,
    string? Error);

public sealed class SuggestLinksRequest
{
    public string Keyword { get; set; } = string.Empty;
    public string? ExcludeContentType { get; set; }
    public Guid? ExcludeId { get; set; }
    public int Take { get; set; } = 10;
}

public sealed class CheckLinksRequest
{
    public IReadOnlyList<string> Urls { get; set; } = [];
}

