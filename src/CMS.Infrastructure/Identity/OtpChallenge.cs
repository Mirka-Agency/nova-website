using CMS.Domain.Common;

namespace CMS.Infrastructure.Identity;

public class OtpChallenge : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime LastSentAtUtc { get; set; }
    public int AttemptCount { get; set; }

    public void MarkUpdated() => Touch();
}
