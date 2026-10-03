using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;
using FluentAssertions;

namespace CMS.Modules.Forms.Application.Tests;

public class FormSchemaRoundTripTests
{
    [Fact]
    public void Serialize_After_Deserialize_With_Object_Config_Does_Not_Throw()
    {
        var original = new FormSchemaDocument
        {
            SchemaVersion = 1,
            Fields = [],
            SubmitBehavior = new FormSubmitBehaviorSchema { Type = "message", Message = "ok" },
            Actions =
            [
                new FormActionSchema
                {
                    Id = "a1",
                    Type = "email_notification",
                    Enabled = true,
                    Config = new Dictionary<string, object?> { ["to"] = "a@b.com", ["enabled"] = true }
                }
            ],
            AntiSpam = new FormAntiSpamSchema
            {
                Enabled = true,
                Provider = "honeypot",
                Config = new Dictionary<string, object?> { ["siteKey"] = "abc" }
            },
            Settings = new FormSettingsSchema { SubmitButtonText = "ارسال" }
        };

        var json1 = FormSchemaLegacyMapper.Serialize(original);
        var parsed = FormSchemaLegacyMapper.TryParse(json1);
        parsed.Should().NotBeNull();

        var form = FormDefinition.Create("Test", "test-key", "test-slug", null);
        form.AddField("choices", "انتخاب", FormFieldType.CheckboxGroup, true, "a:آ|b:ب|c:ج", 1);
        var fieldsDoc = FormSchemaLegacyMapper.ToDocument(form);

        var merged = new FormSchemaDocument
        {
            SchemaVersion = parsed!.SchemaVersion,
            Fields = fieldsDoc.Fields,
            SubmitBehavior = parsed.SubmitBehavior,
            Actions = parsed.Actions,
            AntiSpam = parsed.AntiSpam,
            Settings = parsed.Settings
        };

        var act = () => FormSchemaLegacyMapper.Serialize(merged);
        act.Should().NotThrow();

        var json2 = act();
        json2.Should().Contain("checkbox_group");
        fieldsDoc.Fields.Should().ContainSingle(f => f.Key == "choices" && f.Options.Count == 3);
    }

    [Fact]
    public void ToDocument_Includes_Newly_Added_CheckboxGroup_Options()
    {
        var form = FormDefinition.Create("Test", "test-key2", "test-slug2", null);
        form.AddField("q1", "سوال", FormFieldType.Select, true, "فروش|پشتیبانی|فنی", 1);
        form.AddField("q2", "علایق", FormFieldType.CheckboxGroup, false, "a:آ|b:ب", 2);

        var doc = FormSchemaLegacyMapper.ToDocument(form);
        doc.Fields.Should().HaveCount(2);
        doc.Fields[1].Type.Should().Be("checkbox_group");
        doc.Fields[1].Options.Select(o => o.Label).Should().Equal("آ", "ب");
    }

    [Fact]
    public void Deserialize_Object_Config_Values_Are_JsonElement()
    {
        var json = """{"schemaVersion":1,"fields":[],"submitBehavior":{"type":"message"},"actions":[{"id":"a1","type":"webhook","enabled":true,"config":{"url":"https://x.test","nested":{"a":1}}}],"antiSpam":{"enabled":true,"provider":"honeypot","config":{"siteKey":"k"}},"settings":{"submitButtonText":"ارسال"}}""";
        var parsed = FormSchemaLegacyMapper.TryParse(json);
        parsed.Should().NotBeNull();
        var siteKey = parsed!.AntiSpam.Config["siteKey"];
        siteKey.Should().NotBeNull();
        siteKey!.GetType().Name.Should().Be("JsonElement");

        var act = () => FormSchemaLegacyMapper.Serialize(parsed);
        act.Should().NotThrow();
    }
}
