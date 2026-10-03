using System.Net;
using System.Net.Sockets;

namespace CMS.Modules.Forms.Application.Security;

/// <summary>Blocks webhook URLs that target loopback, link-local, or private networks (SSRF).</summary>
public static class WebhookUrlGuard
{
    public static bool TryValidatePublicHttpsUrl(string? url, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(url))
        {
            error = "آدرس وب‌هوک خالی است.";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            error = "آدرس وب‌هوک نامعتبر است.";
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "وب‌هوک فقط با HTTPS مجاز است.";
            return false;
        }

        if (uri.IsLoopback
            || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            error = "آدرس وب‌هوک به میزبان داخلی مجاز نیست.";
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var ip))
        {
            if (IsPrivateOrReserved(ip))
            {
                error = "آدرس وب‌هوک به شبکه خصوصی مجاز نیست.";
                return false;
            }

            return true;
        }

        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            if (addresses.Length == 0 || addresses.Any(IsPrivateOrReserved))
            {
                error = "آدرس وب‌هوک به شبکه خصوصی resolve می‌شود.";
                return false;
            }
        }
        catch (SocketException)
        {
            error = "میزبان وب‌هوک قابل resolve نیست.";
            return false;
        }

        return true;
    }

    private static bool IsPrivateOrReserved(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;

        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
            return true;

        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        var bytes = ip.GetAddressBytes();
        return bytes[0] switch
        {
            0 => true,
            10 => true,
            127 => true,
            169 when bytes[1] == 254 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            100 when bytes[1] >= 64 && bytes[1] <= 127 => true, // CGNAT
            _ => false
        };
    }
}
