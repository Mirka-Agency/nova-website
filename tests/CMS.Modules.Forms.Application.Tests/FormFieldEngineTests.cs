using CMS.Modules.Forms.Application.Actions;
using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Forms;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Submissions;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormFieldTypeRegistryTests
{
    private readonly FormFieldTypeRegistry _registry = new();

    [Fact]
    public void Resolves_AllLegacyEnumValues()
    {
        foreach (var type in Enum.GetValues<FormFieldType>())
        {
            var handler = _registry.GetForLegacy(type);
            handler.Should().NotBeNull();
            handler.LegacyEnum.Should().Be(type);
            _registry.ToTypeId(type).Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void AdminSelectable_ExcludesCaptcha()
    {
        _registry.AdminSelectable.Should().NotContain(h => h.TypeId == FormFieldTypeIds.Captcha);
        _registry.All.Should().Contain(h => h.TypeId == FormFieldTypeIds.Captcha);
    }
}

public class FormFieldHandlerValidationTests
{
    private readonly FormFieldTypeRegistry _registry = new();

    [Fact]
    public void Email_RejectsInvalid()
    {
        var field = Schema("email", FormFieldType.Email, required: true);
        var handler = _registry.GetForLegacy(FormFieldType.Email);

        var result = handler.Validate(new FormFieldValidationRequest { Field = field, RawValue = "not-an-email" });
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Number_EnforcesMinMax()
    {
        var field = new FormSchemaField
        {
            Id = Guid.NewGuid().ToString("D"),
            Key = "n",
            Type = FormFieldTypeIds.Number,
            Label = "n",
            Required = true,
            Validation = new FormFieldValidationSchema { Required = true, Min = 1, Max = 10 }
        };
        var handler = _registry.GetForLegacy(FormFieldType.Number);

        handler.Validate(new FormFieldValidationRequest { Field = field, RawValue = "0" })
            .IsValid.Should().BeFalse();
        handler.Validate(new FormFieldValidationRequest { Field = field, RawValue = "5" })
            .NormalizedValue.Should().Be("5");
    }

    [Fact]
    public void CheckboxGroup_JoinsPipeSeparatedSelections()
    {
        var field = Schema("g", FormFieldType.CheckboxGroup, required: true, options: ["A", "B", "C"]);
        var handler = _registry.GetForLegacy(FormFieldType.CheckboxGroup);

        var result = handler.Validate(new FormFieldValidationRequest { Field = field, RawValue = "A|C" });
        result.IsValid.Should().BeTrue();
        result.NormalizedValue.Should().Be("A|C");
    }

    [Fact]
    public void LayoutHandlers_SkipPersist()
    {
        foreach (var type in new[] { FormFieldType.Heading, FormFieldType.Paragraph, FormFieldType.Divider })
        {
            var handler = _registry.GetForLegacy(type);
            handler.IsInput.Should().BeFalse();
            var result = handler.Validate(new FormFieldValidationRequest
            {
                Field = Schema("x", type, required: false),
                RawValue = "ignored"
            });
            result.IsValid.Should().BeTrue();
            result.SkipPersist.Should().BeTrue();
        }
    }

    private static FormSchemaField Schema(
        string key,
        FormFieldType type,
        bool required,
        IReadOnlyList<string>? options = null)
    {
        var registry = new FormFieldTypeRegistry();
        return new FormSchemaField
        {
            Id = Guid.NewGuid().ToString("D"),
            Key = key,
            Type = registry.ToTypeId(type),
            Label = key,
            Required = required,
            Validation = new FormFieldValidationSchema { Required = required },
            Options = (options ?? []).Select(o => new FormFieldOptionSchema { Value = o, Label = o }).ToList()
        };
    }
}

public class FormSchemaLegacyMapperTests
{
    [Fact]
    public void ToDocument_ExcludesCaptchaFields_AndUsesHoneypotDefaults()
    {
        var form = FormDefinition.Create("F", "f", null);
        form.AddField("name", "Name", FormFieldType.Text, true, null, 1);
        form.AddField("cap", "Cap", FormFieldType.Captcha, true, null, 2, settingsJson: """{"expected":"4"}""");

        var doc = FormSchemaLegacyMapper.ToDocument(form);
        doc.Fields.Should().ContainSingle(f => f.Key == "name");
        doc.Fields.Should().NotContain(f => f.Type == FormFieldTypeIds.Captcha);
        doc.AntiSpam.Provider.Should().Be(FormAntiSpamProviderIds.Honeypot);
        doc.SubmitBehavior.Type.Should().Be(FormSubmitBehaviorTypes.Message);
        doc.Settings.SubmitButtonText.Should().Be("ارسال");
        doc.Actions.Should().BeEmpty();
    }

    [Fact]
    public void ApplySaveOverrides_WritesActionsSubmitButton_AndStripsPriorActions()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", null);
        form.AddField("email", "Email", FormFieldType.Email, true, null, 1);

        var prior = new FormSchemaDocument
        {
            SchemaVersion = 1,
            Fields =
            [
                new FormSchemaField
                {
                    Id = form.Fields.First().Id.ToString("D"),
                    Key = "email",
                    Type = FormFieldTypeIds.Email,
                    Label = "Email"
                }
            ],
            Actions =
            [
                new FormActionSchema
                {
                    Id = "stale_email",
                    Type = FormActionTypeIds.EmailNotification,
                    Enabled = true,
                    Config = new Dictionary<string, object?> { ["to"] = new[] { "old@example.com" } }
                },
                new FormActionSchema
                {
                    Id = "stale_other",
                    Type = "custom_legacy",
                    Enabled = true,
                    Config = new Dictionary<string, object?>()
                }
            ],
            Settings = new FormSettingsSchema { SubmitButtonText = "Old" }
        };

        var command = new SaveFormCommand(
            Name: "Contact",
            Key: "contact",
            Slug: "contact",
            Description: null,
            Status: FormStatus.Draft,
            SuccessMessage: "Thanks",
            RedirectUrl: null,
            SubmitButtonText: "Send now",
            SendEmailNotification: true,
            NotifyEmail: "ops@example.com",
            NotifyEmailSubject: "New reply",
            NotifySenderName: "Site",
            NotifyReplyToFieldKey: "email",
            NotifyReplyToFieldId: form.Fields.First().Id,
            AutoReplyEnabled: true,
            AutoReplySubject: "We got it",
            AutoReplyBody: "Hi",
            AutoReplyEmailFieldKey: "email",
            AutoReplyEmailFieldId: form.Fields.First().Id,
            EnableCaptcha: false,
            SubmitBehaviorType: FormSubmitBehaviorTypes.Message,
            WebhookEnabled: true,
            WebhookUrl: "https://hooks.example/form",
            WebhookSecret: "s3cret",
            AntiSpamEnabled: true,
            AntiSpamProvider: FormAntiSpamProviderIds.Honeypot);

        var updated = FormSchemaLegacyMapper.ApplySaveOverrides(prior, form, command);

        updated.Settings.SubmitButtonText.Should().Be("Send now");
        updated.SubmitBehavior.Type.Should().Be(FormSubmitBehaviorTypes.Message);
        updated.SubmitBehavior.Message.Should().Be("Thanks");

        updated.Actions.Should().HaveCount(3);
        updated.Actions.Should().NotContain(a => a.Id == "stale_email" || a.Id == "stale_other");
        updated.Actions.Should().ContainSingle(a => a.Type == FormActionTypeIds.EmailNotification);
        updated.Actions.Should().ContainSingle(a => a.Type == FormActionTypeIds.AutoReply);
        updated.Actions.Should().ContainSingle(a => a.Type == FormActionTypeIds.Webhook);

        var notify = updated.Actions.Single(a => a.Type == FormActionTypeIds.EmailNotification);
        FormActionConfigReader.GetStringList(notify.Config, "to").Should().Equal("ops@example.com");

        var webhook = updated.Actions.Single(a => a.Type == FormActionTypeIds.Webhook);
        FormActionConfigReader.GetString(webhook.Config, "url").Should().Be("https://hooks.example/form");
        FormActionConfigReader.GetString(webhook.Config, "secretKey").Should().Be("s3cret");
    }

    [Fact]
    public void ApplySaveOverrides_Persists_NonHoneypot_AntiSpamProvider()
    {
        var form = FormDefinition.Create("Contact", "contact2", "contact-2", null);

        var prior = new FormSchemaDocument
        {
            SchemaVersion = 1,
            Fields = [],
            AntiSpam = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Honeypot
            },
            Settings = new FormSettingsSchema { SubmitButtonText = "ارسال" }
        };

        var command = new SaveFormCommand(
            Name: "Contact",
            Key: "contact2",
            Slug: "contact-2",
            Description: null,
            Status: FormStatus.Draft,
            SuccessMessage: "Thanks",
            RedirectUrl: null,
            SubmitButtonText: "Send",
            SendEmailNotification: false,
            NotifyEmail: null,
            NotifyEmailSubject: null,
            NotifySenderName: null,
            NotifyReplyToFieldKey: null,
            NotifyReplyToFieldId: null,
            AutoReplyEnabled: false,
            AutoReplySubject: null,
            AutoReplyBody: null,
            AutoReplyEmailFieldKey: null,
            AutoReplyEmailFieldId: null,
            EnableCaptcha: false,
            SubmitBehaviorType: FormSubmitBehaviorTypes.Message,
            WebhookEnabled: false,
            WebhookUrl: null,
            WebhookSecret: null,
            AntiSpamEnabled: true,
            AntiSpamProvider: FormAntiSpamProviderIds.Turnstile);

        var updated = FormSchemaLegacyMapper.ApplySaveOverrides(prior, form, command);
        var json = FormSchemaLegacyMapper.Serialize(updated);
        var parsed = FormSchemaLegacyMapper.TryParse(json);

        updated.AntiSpam.Enabled.Should().BeTrue();
        updated.AntiSpam.Provider.Should().Be(FormAntiSpamProviderIds.Turnstile);
        parsed.Should().NotBeNull();
        parsed!.AntiSpam.Provider.Should().Be(FormAntiSpamProviderIds.Turnstile);
    }
}

public class SubmissionDataDocumentTests
{
    [Fact]
    public void RoundTrips_DataAndContext_Json()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        var data = new SubmissionDataDocument
        {
            Fields =
            [
                new SubmissionDataField
                {
                    FieldId = Guid.NewGuid().ToString("D"),
                    Key = "email",
                    Label = "Email",
                    Type = "email",
                    Value = "a@b.com"
                },
                new SubmissionDataField
                {
                    FieldId = Guid.NewGuid().ToString("D"),
                    Key = "resume",
                    Label = "Resume",
                    Type = "file",
                    Value = "https://cdn.example/cv.pdf",
                    FileId = Guid.NewGuid().ToString("D")
                }
            ]
        };

        var context = new SubmissionContextDocument
        {
            Page = "/forms/contact",
            Locale = "fa-IR",
            Referrer = "https://example.com",
            UtmSource = "newsletter",
            UtmMedium = "email",
            UtmCampaign = "spring"
        };

        var dataJson = System.Text.Json.JsonSerializer.Serialize(data, options);
        var contextJson = System.Text.Json.JsonSerializer.Serialize(context, options);

        var dataBack = System.Text.Json.JsonSerializer.Deserialize<SubmissionDataDocument>(dataJson, options);
        var contextBack = System.Text.Json.JsonSerializer.Deserialize<SubmissionContextDocument>(contextJson, options);

        dataBack!.Fields.Should().HaveCount(2);
        dataBack.Fields[0].Key.Should().Be("email");
        dataBack.Fields[1].FileId.Should().NotBeNullOrWhiteSpace();
        contextBack!.Page.Should().Be("/forms/contact");
        contextBack.UtmSource.Should().Be("newsletter");
        contextJson.Should().Contain("utm_source");
    }
}
