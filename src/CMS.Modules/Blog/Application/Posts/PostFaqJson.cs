using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Modules.Blog.Application.Posts;

public sealed record PostFaqItemDto(string Question, string Answer);

public static class PostFaqJson
{
    public const int MaxItems = 50;
    public const int MaxQuestionLength = 500;
    public const int MaxAnswerLength = 5000;
    public const int MaxJsonLength = 100_000;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static IReadOnlyList<PostFaqItemDto> Parse(string? faqJson)
    {
        if (string.IsNullOrWhiteSpace(faqJson))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<FaqItemPayload>>(faqJson, DeserializerOptions);
            if (items is null || items.Count == 0)
                return [];

            return Normalize(items.Select(x => new PostFaqItemDto(x.Question ?? string.Empty, x.Answer ?? string.Empty)));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string? Serialize(IEnumerable<PostFaqItemDto>? items)
    {
        var normalized = Normalize(items ?? []);
        if (normalized.Count == 0)
            return null;

        return JsonSerializer.Serialize(
            normalized.Select(x => new FaqItemPayload { Question = x.Question, Answer = x.Answer }),
            SerializerOptions);
    }

    public static string? NormalizeJson(string? faqJson)
    {
        if (string.IsNullOrWhiteSpace(faqJson))
            return null;

        return Serialize(Parse(faqJson));
    }

    public static bool TryValidate(string? faqJson, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(faqJson))
            return true;

        if (faqJson.Length > MaxJsonLength)
        {
            error = "سوالات متداول خیلی طولانی است.";
            return false;
        }

        List<FaqItemPayload>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<FaqItemPayload>>(faqJson, DeserializerOptions);
        }
        catch (JsonException)
        {
            error = "فرمت سوالات متداول نامعتبر است.";
            return false;
        }

        if (items is null)
            return true;

        if (items.Count > MaxItems)
        {
            error = $"حداکثر {MaxItems} سوال متداول مجاز است.";
            return false;
        }

        foreach (var item in items)
        {
            var question = (item.Question ?? string.Empty).Trim();
            var answer = (item.Answer ?? string.Empty).Trim();
            if (question.Length == 0 && answer.Length == 0)
                continue;
            if (question.Length == 0 || answer.Length == 0)
            {
                error = "برای هر سوال متداول، هم سوال و هم پاسخ الزامی است.";
                return false;
            }

            if (question.Length > MaxQuestionLength)
            {
                error = $"سوال حداکثر {MaxQuestionLength} نویسه می‌تواند باشد.";
                return false;
            }

            if (answer.Length > MaxAnswerLength)
            {
                error = $"پاسخ حداکثر {MaxAnswerLength} نویسه می‌تواند باشد.";
                return false;
            }
        }

        return true;
    }

    private static IReadOnlyList<PostFaqItemDto> Normalize(IEnumerable<PostFaqItemDto> items)
    {
        var result = new List<PostFaqItemDto>();
        foreach (var item in items)
        {
            var question = (item.Question ?? string.Empty).Trim();
            var answer = (item.Answer ?? string.Empty).Trim();
            if (question.Length == 0 || answer.Length == 0)
                continue;

            if (question.Length > MaxQuestionLength)
                question = question[..MaxQuestionLength];
            if (answer.Length > MaxAnswerLength)
                answer = answer[..MaxAnswerLength];

            result.Add(new PostFaqItemDto(question, answer));
            if (result.Count >= MaxItems)
                break;
        }

        return result;
    }

    private sealed class FaqItemPayload
    {
        public string? Question { get; set; }
        public string? Answer { get; set; }
    }
}
