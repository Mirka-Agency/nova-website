using System.Security.Cryptography;
using System.Text;

namespace CMS.Application.Users;

public static class UserAvatarUrl
{
    public static string Resolve(string? avatarUrl, string? email, int size = 80)
    {
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            return avatarUrl.Trim();

        return Gravatar(email, size);
    }

    public static string Gravatar(string? email, int size = 80)
    {
        size = Math.Clamp(size, 1, 2048);
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        var hash = Md5Hex(normalized);
        return $"https://www.gravatar.com/avatar/{hash}?s={size}&d=identicon&r=g";
    }

#pragma warning disable CA5351 // MD5 is required by the Gravatar API
    private static string Md5Hex(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
#pragma warning restore CA5351
}
