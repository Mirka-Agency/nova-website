using CMS.Modules.Forms.Application.AntiSpam;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Application.Public;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormPublicContractMapperTests
{
    [Fact]
    public void From_UsesSchemaTypeIds_AndStripsSecretKey()
    {
        var form = FormDefinition.Create("Contact", "contact", "contact", "desc");
        form.SetStatus(FormStatus.Published);

        var schema = new FormSchemaDocument
        {
            SchemaVersion = 1,
            Fields =
            [
                new FormSchemaField
                {
                    Id = Guid.NewGuid().ToString("D"),
                    Key = "email",
                    Type = FormFieldTypeIds.Email,
                    Label = "Email",
                    Required = true,
                    Position = 1,
                    Layout = new FormFieldLayoutSchema { Width = "half" }
                }
            ],
            SubmitBehavior = new FormSubmitBehaviorSchema
            {
                Type = "redirect",
                Url = "/thanks",
                Message = "ok"
            },
            AntiSpam = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = FormAntiSpamProviderIds.Turnstile,
                Config = new Dictionary<string, object?>
                {
                    ["siteKey"] = "public-site-key",
                    ["secretKey"] = "should-not-leak"
                }
            },
            Settings = new FormSettingsSchema { SubmitButtonText = "Send" }
        };

        var contract = FormPublicContractMapper.From(form, schema, Guid.NewGuid());

        contract.Key.Should().Be("contact");
        contract.SubmitUrl.Should().Be("/forms/contact");
        contract.Settings.SubmitButtonText.Should().Be("Send");
        contract.SubmitBehavior.Type.Should().Be("redirect");
        contract.SubmitBehavior.Url.Should().Be("/thanks");
        contract.AntiSpam.Provider.Should().Be(FormAntiSpamProviderIds.Turnstile);
        contract.AntiSpam.SiteKey.Should().Be("public-site-key");
        contract.Fields.Should().ContainSingle(f => f.Type == FormFieldTypeIds.Email && f.LayoutWidth == "half");

        var json = System.Text.Json.JsonSerializer.Serialize(contract);
        json.Should().NotContain("should-not-leak");
        json.Should().NotContain("secretKey");
    }

    [Fact]
    public void From_FallsBackToLegacyFields_WhenSchemaEmpty()
    {
        var form = FormDefinition.Create("Legacy", "legacy", "legacy", null);
        form.AddField("name", "Name", FormFieldType.Text, true, null, 1);

        var schema = new FormSchemaDocument { Fields = [] };
        var contract = FormPublicContractMapper.From(form, schema);

        contract.Fields.Should().ContainSingle(f =>
            f.Key == "name" && f.Type == FormFieldTypeIds.Text && f.Required);
    }
}
