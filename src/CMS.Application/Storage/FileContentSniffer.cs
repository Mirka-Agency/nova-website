namespace CMS.Application.Storage;

/// <summary>Validates file content against declared MIME using magic-byte signatures.</summary>
public static class FileContentSniffer
{
    public static bool MatchesDeclaredImage(Stream stream, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var header = ReadHeader(stream, 16);
        return contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => header.Length >= 2 && header[0] == 0xFF && header[1] == 0xD8,
            "image/png" => header.Length >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            "image/gif" => header.Length >= 6
                && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
            "image/webp" => header.Length >= 12
                && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }

    public static bool MatchesDeclaredFormFile(Stream stream, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var normalized = contentType.Trim().ToLowerInvariant();
        if (normalized.StartsWith("image/", StringComparison.Ordinal))
            return MatchesDeclaredImage(stream, normalized);

        var header = ReadHeader(stream, 8);
        return normalized switch
        {
            "application/pdf" => header.Length >= 4
                && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,
            "application/msword" => header.Length >= 8
                && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => header.Length >= 4
                && header[0] == 0x50 && header[1] == 0x4B && header[2] == 0x03 && header[3] == 0x04,
            "text/plain" => IsMostlyText(header),
            _ => false
        };
    }

    public static bool MatchesDeclaredVideo(Stream stream, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var header = ReadHeader(stream, 12);
        return contentType.Trim().ToLowerInvariant() switch
        {
            "video/mp4" or "video/quicktime" => IsIsoBaseMediaFtyp(header),
            "video/webm" => header.Length >= 4
                && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            "video/ogg" => header.Length >= 4
                && header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53,
            _ => false
        };
    }

    public static bool MatchesDeclaredAudio(Stream stream, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var header = ReadHeader(stream, 12);
        return contentType.Trim().ToLowerInvariant() switch
        {
            "audio/mpeg" or "audio/mp3" => IsMp3Header(header),
            "audio/ogg" => header.Length >= 4
                && header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53,
            "audio/wav" or "audio/x-wav" => header.Length >= 12
                && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x41 && header[10] == 0x56 && header[11] == 0x45,
            "audio/webm" => header.Length >= 4
                && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            "audio/mp4" or "audio/aac" or "audio/x-m4a" => IsIsoBaseMediaFtyp(header) || IsAdtsAac(header),
            _ => false
        };
    }

    private static bool IsMp3Header(byte[] header)
    {
        if (header.Length >= 3
            && header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33)
            return true;

        return header.Length >= 2
               && header[0] == 0xFF
               && (header[1] & 0xE0) == 0xE0;
    }

    private static bool IsAdtsAac(byte[] header) =>
        header.Length >= 2
        && header[0] == 0xFF
        && (header[1] & 0xF0) == 0xF0;

    private static bool IsIsoBaseMediaFtyp(byte[] header) =>
        header.Length >= 8
        && header[4] == (byte)'f'
        && header[5] == (byte)'t'
        && header[6] == (byte)'y'
        && header[7] == (byte)'p';

    private static byte[] ReadHeader(Stream stream, int count)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must be seekable for content sniffing.");

        var position = stream.Position;
        var buffer = new byte[count];
        var read = stream.Read(buffer, 0, count);
        stream.Position = position;
        return read == buffer.Length ? buffer : buffer[..read];
    }

    private static bool IsMostlyText(byte[] header)
    {
        if (header.Length == 0)
            return false;

        foreach (var b in header)
        {
            if (b is 0x09 or 0x0A or 0x0D or (>= 0x20 and <= 0x7E))
                continue;
            return false;
        }

        return true;
    }
}
