namespace CMS.Web.Models;

public class ErrorViewModel
{
    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "خطا";
    public string Message { get; set; } = "خطای غیرمنتظره‌ای رخ داد.";
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsNotFound => StatusCode == 404;
}
