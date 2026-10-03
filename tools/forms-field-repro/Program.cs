using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Interfaces;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Infrastructure;
using CMS.Modules.Forms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var formIdArg = args.ElementAtOrDefault(0);
var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
         ?? "Host=localhost;Port=5432;Database=mirka_cms;Username=postgres;Password=1234";

var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = cs
    })
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddFormsModule(config);

await using var sp = services.BuildServiceProvider();
await using var scope = sp.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<FormsDbContext>();
var forms = scope.ServiceProvider.GetRequiredService<IFormService>();

var formId = Guid.TryParse(formIdArg, out var parsed)
    ? parsed
    : await db.Forms.OrderByDescending(f => f.CreatedAtUtc).Select(f => f.Id).FirstAsync();

var form = await db.Forms.AsNoTracking()
    .Where(f => f.Id == formId)
    .Select(f => new { f.Id, f.Name, f.Status, f.PublishedVersionId, f.DraftVersionId })
    .FirstOrDefaultAsync();

Console.WriteLine($"Form: {form}");
var versions = await db.FormVersions.AsNoTracking()
    .Where(v => v.FormId == formId)
    .OrderBy(v => v.VersionNumber)
    .Select(v => new { v.Id, v.VersionNumber, v.State })
    .ToListAsync();
Console.WriteLine("Versions:");
foreach (var v in versions)
    Console.WriteLine($"  {v}");

var key = $"repro_{DateTime.UtcNow:HHmmss}";
try
{
    var id = await forms.AddFieldAsync(formId, new SaveFormFieldCommand(
        Key: key,
        Label: "Repro field",
        FieldType: FormFieldType.Select,
        IsRequired: false,
        OptionsCsv: "a|A\nb|B",
        Placeholder: null,
        HelpText: null,
        SettingsJson: null,
        SortOrder: 99));
    Console.WriteLine($"OK added field {id}");
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
    if (ex.InnerException is not null)
        Console.WriteLine($"INNER: {ex.InnerException.Message}");
}
