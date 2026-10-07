namespace CMS.Modules.Forms.Application.Actions;

public static class FormActionTypeIds
{
    public const string EmailNotification = "email_notification";
    public const string AutoReply = "auto_reply";
    public const string Webhook = "webhook";
    public const string WhatsAppNotification = "whatsapp_notification";
}

public static class FormSubmitBehaviorTypes
{
    public const string Message = "message";
    public const string Redirect = "redirect";
    public const string Page = "page";
}
