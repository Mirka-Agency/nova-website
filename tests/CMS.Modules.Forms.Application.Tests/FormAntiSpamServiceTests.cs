using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormAntiSpamServiceTests
{
    private sealed class EmptyCredentials : IFormAntiSpamCredentials
    {
        public string? GetSiteKey(string? providerId) => null;
        public string? GetSecretKey(string? providerId) => null;
        public bool IsConfigured(string? providerId) => false;
    }

    private static FormAntiSpamService CreateService() =>
        new(
            [
                new HoneypotAntiSpamProvider(),
                new SimpleCaptchaAntiSpamProvider()
            ],
            new EmptyCredentials());

    [Fact]
    public async Task Validate_Rejects_Filled_Honeypot()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        var service = CreateService();
        var settings = new FormAntiSpamSchema
        {
            Enabled = true,
            Provider = FormAntiSpamProviderIds.Honeypot
        };

        var result = await service.ValidateAsync(new FormAntiSpamValidationRequest
        {
            Settings = settings,
            Form = form,
            HoneypotValue = "http://spam.example"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("honeypot");
    }

    [Fact]
    public async Task SimpleCaptcha_Requires_Answer()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        var service = CreateService();
        var settings = new FormAntiSpamSchema
        {
            Enabled = true,
            Provider = FormAntiSpamProviderIds.SimpleCaptcha,
            Config = new Dictionary<string, object?> { ["expected"] = "42" }
        };

        var result = await service.ValidateAsync(new FormAntiSpamValidationRequest
        {
            Settings = settings,
            Form = form,
            CaptchaAnswer = null
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Values.SelectMany(v => v).Should().Contain(m => m.Contains("کپچا"));
    }

    [Fact]
    public void ResolveSettings_Uses_Schema_SimpleCaptcha()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        var schema = new FormSchemaDocument
        {
            AntiSpam = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.SimpleCaptcha,
                Config = new Dictionary<string, object?> { ["expected"] = "42" }
            }
        };

        var service = CreateService();
        var settings = service.ResolveSettings(form, schema);

        settings.Enabled.Should().BeTrue();
        settings.Provider.Should().Be(FormAntiSpamProviderIds.SimpleCaptcha);
    }

    [Fact]
    public void ResolveSettings_Defaults_To_Honeypot_When_Schema_Missing()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        var service = CreateService();
        var settings = service.ResolveSettings(form, schema: null);

        settings.Enabled.Should().BeTrue();
        settings.Provider.Should().Be(FormAntiSpamProviderIds.Honeypot);
    }

    [Fact]
    public async Task External_Rejects_When_SecretKey_Missing()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        var provider = new ExternalChallengeAntiSpamProvider(
            FormAntiSpamProviderIds.Turnstile,
            "https://example.test/verify",
            logger);

        var result = await provider.ValidateAsync(new FormAntiSpamValidationRequest
        {
            Settings = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Turnstile,
                Config = new Dictionary<string, object?> { ["siteKey"] = "public-key" }
            },
            Form = form,
            ProviderToken = "any-token"
        });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainKey("antispam");
        result.Errors["antispam"].Should().Contain(m => m.Contains("Secret Key"));
    }
}
