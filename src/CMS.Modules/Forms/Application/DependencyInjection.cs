using System.Reflection;
using CMS.Application.Security;
using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Fields;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMS.Modules.Forms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFormsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton<IFormFieldTypeRegistry, FormFieldTypeRegistry>();

        services.AddScoped<IFormActionHandler, EmailNotificationActionHandler>();
        services.AddScoped<IFormActionHandler, AutoReplyActionHandler>();
        services.AddScoped<IFormActionHandler, WebhookActionHandler>();
        services.AddScoped<IFormActionHandler, WhatsAppNotificationActionHandler>();
        services.AddScoped<IFormActionExecutor, FormActionExecutor>();

        services.AddSingleton<IFormAntiSpamCredentials, FormAntiSpamCredentials>();
        services.AddScoped<IFormAntiSpamProvider, HoneypotAntiSpamProvider>();
        services.AddScoped<IFormAntiSpamProvider, SimpleCaptchaAntiSpamProvider>();
        services.AddScoped<IFormAntiSpamProvider>(sp =>
            new ExternalChallengeAntiSpamProvider(
                FormAntiSpamProviderIds.Turnstile,
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                sp.GetRequiredService<ILoggerFactory>().CreateLogger("Forms.AntiSpam.Turnstile"),
                sp.GetService<IHttpClientFactory>()));
        services.AddScoped<IFormAntiSpamProvider>(sp =>
            new ExternalChallengeAntiSpamProvider(
                FormAntiSpamProviderIds.Recaptcha,
                "https://www.google.com/recaptcha/api/siteverify",
                sp.GetRequiredService<ILoggerFactory>().CreateLogger("Forms.AntiSpam.Recaptcha"),
                sp.GetService<IHttpClientFactory>(),
                sp.GetRequiredService<ISharedCaptchaVerifier>(),
                SharedCaptchaActions.Form));
        services.AddScoped<IFormAntiSpamProvider>(sp =>
            new ExternalChallengeAntiSpamProvider(
                FormAntiSpamProviderIds.Hcaptcha,
                "https://hcaptcha.com/siteverify",
                sp.GetRequiredService<ILoggerFactory>().CreateLogger("Forms.AntiSpam.Hcaptcha"),
                sp.GetService<IHttpClientFactory>()));
        services.AddScoped<IFormAntiSpamService, FormAntiSpamService>();

        return services;
    }
}
