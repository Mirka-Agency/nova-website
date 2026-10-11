namespace CMS.Modules.Comments.Application.Import;

public interface ICommentUrlMatcher
{
    string? NormalizePath(string? pageUrl, IReadOnlyCollection<string> trustedHosts);

    CommentUrlMatchResult Match(
        string? pageUrl,
        IReadOnlyList<CommentTargetCandidate> catalog,
        IReadOnlyCollection<string> trustedHosts);
}
