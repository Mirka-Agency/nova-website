namespace CMS.Modules.Forms.Domain.Fields;

/// <summary>
/// Parses admin OptionsCsv tokens. Supports <c>value:label</c> / <c>value=label</c>;
/// plain tokens keep value == label. Separator between options remains <c>|</c>.
/// </summary>
public static class FormFieldOptionParser
{
    public readonly record struct OptionPair(string Value, string Label);

    public static IReadOnlyList<OptionPair> Parse(string? optionsCsv)
    {
        if (string.IsNullOrWhiteSpace(optionsCsv))
            return [];

        return optionsCsv
            .Split(['|', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(ParseToken)
            .ToList();
    }

    public static OptionPair ParseToken(string token)
    {
        var trimmed = token.Trim();
        var sep = trimmed.IndexOfAny([':', '=']);
        if (sep <= 0 || sep >= trimmed.Length - 1)
            return new OptionPair(trimmed, trimmed);

        var value = trimmed[..sep].Trim();
        var label = trimmed[(sep + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(label))
            return new OptionPair(trimmed, trimmed);

        return new OptionPair(value, label);
    }

    public static string? Normalize(string? optionsCsv)
    {
        var pairs = Parse(optionsCsv);
        if (pairs.Count == 0)
            return null;

        return string.Join('|', pairs.Select(p =>
            string.Equals(p.Value, p.Label, StringComparison.Ordinal)
                ? p.Value
                : $"{p.Value}:{p.Label}"));
    }

    /// <summary>
    /// Maps stored option value(s) to their labels (e.g. <c>yes</c> → <c>بله</c>).
    /// Multi-select values separated by <c>|</c> or <c>,</c> are remapped individually.
    /// </summary>
    public static string? ToDisplayLabels(string? storedValue, IReadOnlyList<OptionPair> options)
    {
        if (string.IsNullOrWhiteSpace(storedValue) || options.Count == 0)
            return storedValue;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Value))
                continue;
            if (!map.ContainsKey(option.Value))
                map[option.Value] = string.IsNullOrWhiteSpace(option.Label) ? option.Value : option.Label;
        }

        if (map.Count == 0)
            return storedValue;

        var trimmed = storedValue.Trim();
        if (map.TryGetValue(trimmed, out var singleLabel))
            return singleLabel;

        if (trimmed.IndexOfAny(['|', ',', '\n', '\r']) < 0)
            return storedValue;

        var parts = trimmed.Split(
            ['|', ',', '\n', '\r'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return storedValue;

        return string.Join('|', parts.Select(part =>
            map.TryGetValue(part, out var label) ? label : part));
    }
}
