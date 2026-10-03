namespace CMS.Application.Users;

public interface IUserAccountLookup
{
    Task<string?> FindIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<string?> GetEmailAsync(string userId, CancellationToken cancellationToken = default);
}
