namespace CMS.Application.Users;

public sealed record ContentAuthorOptionDto(string Id, string DisplayName);

public interface IContentAuthorLookup
{
    Task<IReadOnlyList<ContentAuthorOptionDto>> ListContentAuthorsAsync(
        CancellationToken cancellationToken = default);

    Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken = default);
}
