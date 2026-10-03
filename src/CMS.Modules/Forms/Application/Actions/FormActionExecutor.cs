using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Submissions;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Application.Actions;

public sealed class FormActionExecutor : IFormActionExecutor
{
    private readonly IReadOnlyDictionary<string, IFormActionHandler> _handlers;
    private readonly ILogger<FormActionExecutor> _logger;

    public FormActionExecutor(
        IEnumerable<IFormActionHandler> handlers,
        ILogger<FormActionExecutor> logger)
    {
        _handlers = handlers.ToDictionary(h => h.TypeId, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task ExecuteAllAsync(
        FormActionExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var action in context.Schema.Actions.Where(a => a.Enabled))
        {
            if (!_handlers.TryGetValue(action.Type, out var handler))
            {
                _logger.LogWarning(
                    "No handler for form action type {ActionType} ({ActionId})",
                    action.Type,
                    action.Id);
                continue;
            }

            try
            {
                await handler.ExecuteAsync(action, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Form action {ActionType}/{ActionId} failed for submission {SubmissionId}",
                    action.Type,
                    action.Id,
                    context.Submission.Id);
            }
        }
    }
}

public static class FormActionExecutionContextFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static FormActionExecutionContext Create(
        Domain.Entities.FormDefinition form,
        Domain.Entities.FormSubmission submission,
        FormSchemaDocument schema,
        string? siteName = null,
        string? pageUrl = null)
    {
        var byKey = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var byId = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var data = JsonSerializer.Deserialize<SubmissionDataDocument>(submission.DataJson, JsonOptions);
            if (data?.Fields is { Count: > 0 })
            {
                var schemaByKey = schema.Fields
                    .Where(f => !string.IsNullOrWhiteSpace(f.Key))
                    .GroupBy(f => f.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                foreach (var field in data.Fields)
                {
                    var display = SubmissionValueDisplay.Resolve(field, schemaByKey);
                    byKey[field.Key] = display;
                    if (!string.IsNullOrWhiteSpace(field.FieldId))
                        byId[field.FieldId] = display;
                }
            }
        }
        catch (JsonException)
        {
            // Leave dictionaries empty when DataJson is missing or invalid.
        }

        return new FormActionExecutionContext
        {
            Form = form,
            Submission = submission,
            Schema = schema,
            FieldValuesByKey = byKey,
            FieldValuesById = byId,
            SiteName = siteName,
            PageUrl = pageUrl
        };
    }
}
