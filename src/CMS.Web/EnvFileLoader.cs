namespace CMS.Web;

/// <summary>
/// Loads a repo-root <c>.env</c> into process environment variables so
/// <c>Storage__S3__*</c> / connection strings work with <c>dotnet run</c>
/// (ASP.NET Core does not read <c>.env</c> by itself).
/// Existing environment variables are never overwritten.
/// </summary>
internal static class EnvFileLoader
{
    public static void LoadNearest()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                var path = Path.Combine(dir.FullName, ".env");
                if (File.Exists(path))
                {
                    LoadFile(path);
                    return;
                }

                dir = dir.Parent;
            }
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            if (key.Length == 0)
                continue;

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                continue;

            var value = Unquote(line[(eq + 1)..].Trim());
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
