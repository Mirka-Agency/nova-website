using System.Text.Json;
using System.Text.Json.Serialization;
using CMS.Modules.Forms.Application.Schema;
using CMS.Modules.Forms.Application.Fields;
using CMS.Modules.Forms.Domain.Entities;
using CMS.Modules.Forms.Domain.Enums;

var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

// Simulate schema with antispam config (object? values)
var original = new FormSchemaDocument
{
    SchemaVersion = 1,
    Fields = [],
    SubmitBehavior = new FormSubmitBehaviorSchema { Type = \"message\", Message = \"ok\" },
    Actions =
    [
        new FormActionSchema
        {
            Id = \"a1\",
            Type = \"email_notification\",
            Enabled = true,
            Config = new Dictionary<string, object?> { [\"to\"] = \"a@b.com\", [\"enabled\"] = true }
        }
    ],
    AntiSpam = new FormAntiSpamSchema
    {
        Enabled = true,
        Provider = \"honeypot\",
        Config = new Dictionary<string, object?> { [\"siteKey\"] = \"abc\" }
    },
    Settings = new FormSettingsSchema { SubmitButtonText = \"ارسال\" }
};

var json1 = JsonSerializer.Serialize(original, jsonOpts);
Console.WriteLine(\"SER1 OK: \" + json1.Length);
var parsed = JsonSerializer.Deserialize<FormSchemaDocument>(json1, jsonOpts);
Console.WriteLine(\"DESER OK, actions=\" + parsed!.Actions.Count + \" configType=\" + parsed.AntiSpam.Config[\"siteKey\"]?.GetType().FullName);

try {
  var json2 = JsonSerializer.Serialize(parsed, jsonOpts);
  Console.WriteLine(\"SER2 OK: \" + json2.Length);
} catch (Exception ex) {
  Console.WriteLine(\"SER2 FAIL: \" + ex.GetType().Name + \": \" + ex.Message);
}

// Also test field with options via FromEntity path
var form = FormDefinition.Create(\"Test\", \"test\", \"test\", null);
form.AddField(\"choices\", \"انتخاب\", FormFieldType.CheckboxGroup, true, \"a:آ|b:ب|c:ج\", 1);
var doc = FormSchemaLegacyMapper.ToDocument(form);
var merged = new FormSchemaDocument {
  SchemaVersion = parsed.SchemaVersion,
  Fields = doc.Fields,
  SubmitBehavior = parsed.SubmitBehavior,
  Actions = parsed.Actions,
  AntiSpam = parsed.AntiSpam,
  Settings = parsed.Settings
};
try {
  var json3 = FormSchemaLegacyMapper.Serialize(merged);
  Console.WriteLine(\"MERGE SER OK: \" + json3.Length);
  Console.WriteLine(json3.Substring(0, Math.Min(300, json3.Length)));
} catch (Exception ex) {
  Console.WriteLine(\"MERGE SER FAIL: \" + ex.GetType().Name + \": \" + ex.Message);
  Console.WriteLine(ex);
}
