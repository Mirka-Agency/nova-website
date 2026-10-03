using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using CMS.Modules.Forms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Infrastructure.Services;

/// <summary>
/// Idempotent seeder for <see cref="SystemFormCatalog"/> definitions.
/// Creates missing forms/fields; does not overwrite admin copy on existing forms.
/// </summary>
public static class FormSystemSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FormsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Forms.SystemSeeder");

        foreach (var definition in SystemFormCatalog.All)
        {
            try
            {
                await EnsureFormAsync(db, definition, logger, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed system form {FormKey}", definition.Key);
                throw;
            }
        }
    }

    private static async Task EnsureFormAsync(
        FormsDbContext db,
        SystemFormDefinition definition,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var key = FormDefinition.NormalizeKey(definition.Key);
        var existing = await db.Forms
            .Include(f => f.Fields)
            .Include(f => f.Versions)
            .FirstOrDefaultAsync(f => f.Key == key, cancellationToken);

        if (existing is null)
        {
            var form = FormDefinition.Create(definition.Name, key, definition.Slug, definition.Description);
            form.MarkAsSystem();
            if (definition.Publish)
                form.Publish();

            foreach (var field in definition.Fields.OrderBy(f => f.Order))
            {
                form.AddField(
                    field.Key,
                    field.Label,
                    field.Type,
                    field.Required,
                    field.OptionsCsv,
                    field.Order,
                    field.Placeholder,
                    field.HelpText,
                    settingsJson: null,
                    isSystem: true);
            }

            db.Forms.Add(form);
            await db.SaveChangesAsync(cancellationToken);
            await FormVersionSync.SyncAsync(db, form, cancellationToken);
            ApplySeedSchemaDefaults(form, definition);
            if (definition.Publish)
                await FormVersionSync.PublishWorkingAsync(db, form, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Seeded system form {FormKey} ({FormId})", form.Key, form.Id);
            return;
        }

        var changed = false;
        if (!existing.IsSystem)
        {
            existing.MarkAsSystem();
            changed = true;
            logger.LogInformation("Marked existing form {FormKey} as system", existing.Key);
        }

        foreach (var fieldDef in definition.Fields.OrderBy(f => f.Order))
        {
            var fieldKey = fieldDef.Key.Trim().ToLowerInvariant();
            if (existing.Fields.Any(f => f.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase)))
            {
                var match = existing.Fields.First(f =>
                    f.Key.Equals(fieldKey, StringComparison.OrdinalIgnoreCase));
                if (!match.IsSystem)
                {
                    match.MarkAsSystem();
                    changed = true;
                }

                continue;
            }

            existing.AddField(
                fieldDef.Key,
                fieldDef.Label,
                fieldDef.Type,
                fieldDef.Required,
                fieldDef.OptionsCsv,
                fieldDef.Order,
                fieldDef.Placeholder,
                fieldDef.HelpText,
                settingsJson: null,
                isSystem: true);
            changed = true;
            logger.LogInformation(
                "Added missing system field {FieldKey} to form {FormKey}",
                fieldKey,
                existing.Key);
        }

        if (!changed)
            return;

        await FormVersionSync.SyncAsync(db, existing, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplySeedSchemaDefaults(FormDefinition form, SystemFormDefinition definition)
    {
        var version = form.DraftVersionId is Guid draftId
            ? form.Versions.FirstOrDefault(v => v.Id == draftId)
            : form.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (version is null)
            return;

        var schema = FormSchemaLegacyMapper.TryParse(version.SchemaJson)
                     ?? FormSchemaLegacyMapper.ToDocument(form);

        var updated = FormSchemaLegacyMapper.ApplySaveOverrides(
            schema,
            form,
            submitBehaviorType: "message",
            successMessage: definition.SuccessMessage ?? "پیام شما با موفقیت ارسال شد.",
            redirectUrl: null,
            submitButtonText: definition.SubmitButtonText ?? "ارسال",
            sendEmailNotification: false,
            notifyEmail: null,
            notifyEmailSubject: null,
            notifySenderName: null,
            notifyReplyToFieldKey: null,
            autoReplyEnabled: false,
            autoReplySubject: null,
            autoReplyBody: null,
            autoReplyEmailFieldKey: null,
            webhookEnabled: false,
            webhookUrl: null,
            webhookSecret: null,
            antiSpamEnabled: true,
            antiSpamProvider: "honeypot",
            antiSpamSiteKey: null,
            antiSpamSecretKey: null,
            simpleCaptchaExpected: null);

        version.ReplaceSchema(FormSchemaLegacyMapper.Serialize(updated));
    }
}
