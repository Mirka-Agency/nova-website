namespace CMS.Modules.Team.Application.TeamItems;

public static class TeamHighlights
{
    public const int MaxItems = 10;
    public const int MaxItemLength = 200;
    public const int MaxTextLength = 2000;

    public static IReadOnlyList<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var result = new List<string>();
        foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var item = line.Trim();
            if (item.Length == 0)
                continue;

            if (item.Length > MaxItemLength)
                item = item[..MaxItemLength];

            result.Add(item);
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    public static string? Normalize(string? text)
    {
        var items = Parse(text);
        if (items.Count == 0)
            return null;

        return string.Join('\n', items);
    }
}
