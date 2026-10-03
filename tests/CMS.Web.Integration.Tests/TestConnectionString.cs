using Microsoft.Extensions.Configuration;

namespace CMS.Web.Integration.Tests;

internal static class TestConnectionString
{
    public const string DatabaseName = "mirka_cms_test";

    /// <summary>
    /// Prefers ConnectionStrings__DefaultConnection, then CMS.Web user-secrets.
    /// Always rewrites Database= to <see cref="DatabaseName"/>.
    /// Returns empty when only the CHANGE_ME placeholder is available.
    /// </summary>
    public static string Resolve()
    {
        var env = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(env) &&
            !env.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return EnsureTestDatabase(env);

        var config = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var current = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(current) ||
            current.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return EnsureTestDatabase(current);
    }

    public static string EnsureTestDatabase(string connectionString)
    {
        if (connectionString.Contains("Database=", StringComparison.OrdinalIgnoreCase))
        {
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Database=[^;]+",
                $"Database={DatabaseName}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return connectionString.TrimEnd(';') + $";Database={DatabaseName}";
    }
}
