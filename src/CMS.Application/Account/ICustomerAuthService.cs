namespace CMS.Application.Account;

public interface ICustomerAuthService
{
    Task<RequestOtpResult> RequestOtpAsync(string phone, CancellationToken cancellationToken = default);
    Task<VerifyOtpResult> VerifyOtpAsync(string phone, string code, CancellationToken cancellationToken = default);
    Task SignOutAsync(CancellationToken cancellationToken = default);
}

public sealed record RequestOtpResult(bool Succeeded, string? Error, int? RetryAfterSeconds = null);
public sealed record VerifyOtpResult(bool Succeeded, string? Error, string? UserId = null, bool IsNewUser = false);
