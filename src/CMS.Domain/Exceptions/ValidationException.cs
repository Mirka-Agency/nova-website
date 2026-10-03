namespace CMS.Domain.Exceptions;

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("یک یا چند خطای اعتبارسنجی رخ داد.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}
