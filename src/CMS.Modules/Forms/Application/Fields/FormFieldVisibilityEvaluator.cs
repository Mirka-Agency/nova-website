using CMS.Modules.Forms.Application.Schema;

namespace CMS.Modules.Forms.Application.Fields;

/// <summary>
/// Evaluates <see cref="FormFieldVisibilitySchema"/> against submitted values.
/// </summary>
public static class FormFieldVisibilityEvaluator
{
    public static bool IsVisible(
        FormSchemaField field,
        IReadOnlyDictionary<string, string?> valuesByFieldIdOrKey,
        IReadOnlyList<FormSchemaField> allFields)
    {
        var visibility = field.Visibility;
        if (visibility?.Conditions is not { Count: > 0 })
            return true;

        var mode = string.IsNullOrWhiteSpace(visibility.Mode)
            ? "all"
            : visibility.Mode.Trim().ToLowerInvariant();

        if (mode == "any")
            return visibility.Conditions.Any(c => Evaluate(c, valuesByFieldIdOrKey, allFields));

        return visibility.Conditions.All(c => Evaluate(c, valuesByFieldIdOrKey, allFields));
    }

    private static bool Evaluate(
        FormVisibilityConditionSchema condition,
        IReadOnlyDictionary<string, string?> valuesByFieldIdOrKey,
        IReadOnlyList<FormSchemaField> allFields)
    {
        var actual = ResolveValue(condition.FieldId, valuesByFieldIdOrKey, allFields);
        var op = string.IsNullOrWhiteSpace(condition.Operator)
            ? "equals"
            : condition.Operator.Trim().ToLowerInvariant();
        var expected = condition.Value;

        return op switch
        {
            "equals" => string.Equals(Normalize(actual), Normalize(expected), StringComparison.OrdinalIgnoreCase),
            "not_equals" => !string.Equals(Normalize(actual), Normalize(expected), StringComparison.OrdinalIgnoreCase),
            "contains" => (actual ?? string.Empty).Contains(expected ?? string.Empty, StringComparison.OrdinalIgnoreCase),
            "is_empty" => string.IsNullOrWhiteSpace(actual),
            "is_not_empty" => !string.IsNullOrWhiteSpace(actual),
            _ => false
        };
    }

    private static string? ResolveValue(
        string fieldId,
        IReadOnlyDictionary<string, string?> valuesByFieldIdOrKey,
        IReadOnlyList<FormSchemaField> allFields)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
            return null;

        if (valuesByFieldIdOrKey.TryGetValue(fieldId, out var direct))
            return direct;

        var target = allFields.FirstOrDefault(f =>
            string.Equals(f.Id, fieldId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.Key, fieldId, StringComparison.OrdinalIgnoreCase));

        if (target is null)
            return null;

        if (!string.IsNullOrWhiteSpace(target.Id)
            && valuesByFieldIdOrKey.TryGetValue(target.Id, out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(target.Key)
            && valuesByFieldIdOrKey.TryGetValue(target.Key, out var byKey))
            return byKey;

        return null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
