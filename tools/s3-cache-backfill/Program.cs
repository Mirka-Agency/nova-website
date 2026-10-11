using CMS.Application.Storage;
using CMS.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// One-shot: apply Cache-Control metadata to existing public S3 objects (same keys/URLs).
LoadDotEnv(FindRepoRoot());

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
services.Configure<S3ObjectStorageOptions>(configuration.GetSection(S3ObjectStorageOptions.SectionName));
services.AddSingleton<IObjectStorage, S3ObjectStorage>();

await using var provider = services.BuildServiceProvider();
var storage = provider.GetRequiredService<IObjectStorage>();
var options = provider.GetRequiredService<IOptions<S3ObjectStorageOptions>>().Value;

Console.WriteLine($"Bucket: {options.BucketName}");
Console.WriteLine($"Endpoint: {options.ServiceUrl}");
Console.WriteLine("Starting Cache-Control backfill…");

var result = await storage.BackfillCacheHeadersAsync();
Console.WriteLine(
    $"Done. examined={result.Examined}, updated={result.Updated}, skipped={result.Skipped}, failed={result.Failed}");

return result.Failed > 0 ? 1 : 0;

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, ".env"))
            || File.Exists(Path.Combine(dir.FullName, "CMS.sln"))
            || File.Exists(Path.Combine(dir.FullName, "nova-website.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}

static void LoadDotEnv(string root)
{
    var path = Path.Combine(root, ".env");
    if (!File.Exists(path))
        return;

    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
            continue;

        var eq = line.IndexOf('=');
        if (eq <= 0)
            continue;

        var key = line[..eq].Trim();
        var value = line[(eq + 1)..].Trim();
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            value = value[1..^1];

        // Do not override real process env (same as CMS.Web EnvFileLoader).
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value);
    }
}
