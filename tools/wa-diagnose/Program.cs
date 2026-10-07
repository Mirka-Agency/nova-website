using System.Text.Json;
using CMS.Infrastructure.Persistence;
using CMS.Infrastructure.WhatsApp;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Infrastructure;
using CMS.Modules.Forms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Prefer process env, then repo-root .env (same as CMS.Web), then user-secrets.
static void LoadEnvFile()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var path = Path.Combine(dir.FullName, ".env");
            if (File.Exists(path))
            {
                foreach (var raw in File.ReadLines(path))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith('#')) continue;
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var key = line[..eq].Trim();
                    if (key.Length == 0) continue;
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))) continue;
                    var value = line[(eq + 1)..].Trim();
                    if (value.Length >= 2 &&
                        ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                        value = value[1..^1];
                    Environment.SetEnvironmentVariable(key, value);
                }
                return;
            }
            dir = dir.Parent;
        }
    }
}

LoadEnvFile();

var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
{
    try
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "user-secrets list --project \"D:\\work\\nova-website\\src\\CMS.Web\\CMS.Web.csproj\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        foreach (var line in output.Split('\n'))
        {
            const string key = "ConnectionStrings:DefaultConnection =";
            if (line.StartsWith(key, StringComparison.Ordinal))
            {
                cs = line[key.Length..].Trim();
                break;
            }
        }
    }
    catch { /* ignore */ }
}

cs ??= "Host=localhost;Port=5432;Database=mirka_cms;Username=postgres;Password=1234";
var dbName = cs.Split(';').Select(p => p.Trim()).FirstOrDefault(p => p.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1] ?? "?";
Console.WriteLine($"DB={dbName} (cs len={cs.Length})");

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = cs
    })
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Error));
services.AddFormsModule(config);
services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));

await using var sp = services.BuildServiceProvider();
await using var scope = sp.CreateAsyncScope();
var formsDb = scope.ServiceProvider.GetRequiredService<FormsDbContext>();
var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Applying ApplicationDbContext migrations...");
    await appDb.Database.MigrateAsync();
    Console.WriteLine("Migrate done.");
}

if (args.Contains("--enable-wa", StringComparer.OrdinalIgnoreCase))
{
    var entity = await appDb.WhatsAppSettings.FirstOrDefaultAsync(x => x.Id == WhatsAppSettings.SingletonId);
    if (entity is null)
    {
        entity = WhatsAppSettings.CreateDefault();
        appDb.WhatsAppSettings.Add(entity);
    }

    entity.Update(
        enabled: true,
        defaultGroupId: entity.DefaultGroupId,
        defaultGroupName: entity.DefaultGroupName,
        defaultTemplate: entity.DefaultTemplate);
    await appDb.SaveChangesAsync();
    Console.WriteLine($"ENABLED WhatsApp: group={entity.DefaultGroupId ?? "(none)"} name={entity.DefaultGroupName ?? "(none)"}");
}

Console.WriteLine("=== Pending / applied WhatsApp migration ===");
var applied = await appDb.Database.GetAppliedMigrationsAsync();
var pending = await appDb.Database.GetPendingMigrationsAsync();
Console.WriteLine("Applied WhatsApp-related: " + string.Join(", ", applied.Where(x => x.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase))));
Console.WriteLine("Pending: " + string.Join(", ", pending));

Console.WriteLine("=== WhatsApp settings (core) ===");
try
{
    var wa = await appDb.WhatsAppSettings.AsNoTracking().FirstOrDefaultAsync();
    if (wa is null)
    {
        Console.WriteLine("NO WhatsAppSettings row");
    }
    else
    {
        Console.WriteLine($"Enabled={wa.Enabled}");
        Console.WriteLine($"DefaultGroupId={wa.DefaultGroupId ?? "(null)"}");
        Console.WriteLine($"DefaultGroupName={wa.DefaultGroupName ?? "(null)"}");
        Console.WriteLine($"TemplateLen={(wa.DefaultTemplate ?? "").Length}");
        Console.WriteLine($"LastSuccessfulSendAtUtc={wa.LastSuccessfulSendAtUtc?.ToString("o") ?? "(null)"}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("WhatsAppSettings query FAILED: " + ex.Message);
}

Console.WriteLine();
Console.WriteLine("=== Forms (booking/consult) ===");
var forms = await formsDb.Forms.AsNoTracking()
    .Include(f => f.Versions)
    .Where(f =>
        f.Key == "booking" || f.Slug == "booking"
        || f.Key == "consulting" || f.Slug == "consulting"
        || f.Key == "consultation" || f.Slug == "consultation"
        || f.Name.Contains("مشاوره") || f.Name.Contains("نوبت"))
    .ToListAsync();
Console.WriteLine("All form keys: " + string.Join(", ",
    await formsDb.Forms.AsNoTracking().Select(f => f.Key + "/" + f.Slug).ToListAsync()));

foreach (var form in forms)
{
    Console.WriteLine($"Form key={form.Key} slug={form.Slug} name={form.Name} status={form.Status}");
    Console.WriteLine($"  PublishedVersionId={form.PublishedVersionId}");
    var published = form.Versions.FirstOrDefault(v => v.Id == form.PublishedVersionId)
                    ?? form.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
    if (published is null)
    {
        Console.WriteLine("  NO version");
        continue;
    }

    var schema = FormSchemaLegacyMapper.TryParse(published.SchemaJson);
    var actions = schema?.Actions ?? [];
    Console.WriteLine($"  Version #{published.VersionNumber} actions={actions.Count}");
    foreach (var a in actions)
    {
        var groupId = FormActionConfigReader.GetString(a.Config, "groupId");
        var groupName = FormActionConfigReader.GetString(a.Config, "groupName");
        Console.WriteLine($"   - {a.Type} enabled={a.Enabled} groupId={groupId ?? "(null)"} groupName={groupName ?? "(null)"}");
    }

    var hasWa = actions.Any(a =>
        a.Enabled &&
        string.Equals(a.Type, FormActionTypeIds.WhatsAppNotification, StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"  HAS_WHATSAPP_ACTION={hasWa}");
}

Console.WriteLine();
Console.WriteLine("=== Recent WhatsApp message logs ===");
try
{
    var logs = await appDb.MessageLogs.AsNoTracking()
        .Where(x => x.Channel == CMS.Application.Messaging.MessageChannel.WhatsApp)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(8)
        .ToListAsync();

    if (logs.Count == 0)
        Console.WriteLine("(none)");
    foreach (var l in logs)
        Console.WriteLine($"{l.CreatedAtUtc:u} status={l.Status} subject={l.Subject} to={l.Recipient} err={l.ErrorMessage}");
}
catch (Exception ex)
{
    Console.WriteLine("MessageLogs query FAILED: " + ex.Message);
}
